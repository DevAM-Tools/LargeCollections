/*
MIT License
SPDX-License-Identifier: MIT

Copyright (c) 2025 DevAM

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LargeCollections.DiskCache;

/// <summary>
/// A dictionary-like collection that allows to limit the amount of memory (RAM) in MB that will be used.
/// Any memory requirement that exceeds this amount is automatically swapped out to disk.
/// Additionally it offers multi-threaded operations for performance improvements.
/// If <see cref="long"/>,<see cref="string"/> or <see cref="byte"/>[] is used for <see cref="TKey"/> and/or <see cref="TValue"/>
/// it will have a performance benefit due to a specialized implementaion.
/// If other types are used functions for serialization and deserialization must be provided so that keys and/or values can be stored on disk if needed.
/// </summary>
[DebuggerDisplay("DiskCache")]
public class DiskCache<TKey, TValue> : IDiskCache<TKey, TValue>, IDisposable where TKey : notnull
{
    public long MaxMemorySize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set;
    }

    public byte DegreeOfParallelism
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected set;
    }

    /// <summary>
    /// The base file path for the cache files.
    /// </summary>
    public readonly string BaseFilePath;

    /// <summary>
    /// The file extension for the cache files.
    /// </summary>
    public readonly string FileExtension;

    /// <summary>
    /// An enumeration of paths to files that are in use.
    /// </summary>
    public IEnumerable<string> FilePaths
    {
        get
        {
            for (byte i = 0; i < DegreeOfParallelism; i++)
            {
                yield return GetFilePath(i);
            }
        }
    }

    /// <summary>
    /// Gets the file path for the specified index.
    /// </summary>
    /// <param name="index">The 0 based index of the file.</param>
    /// <returns></returns>
    public string GetFilePath(byte index)
    {
        return $"{BaseFilePath}_{index}.{FileExtension}";
    }

    /// <summary>
    /// If true any operation that would change the collection will throw an <see cref="InvalidOperationException"/>.
    /// </summary>
    public readonly bool IsReadOnly;

    /// <summary>
    /// If true existing files will be overwriten on creation.
    /// </summary>
    public readonly bool OverwriteExistingFiles;

    /// <summary>
    /// If true the file that was created on the disk will be removed when 'Dispose()' will be called.
    /// </summary>
    public readonly bool DeleteFilesOnDispose;

    public IEnumerable<TKey> Keys
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            foreach (KeyValuePair<TKey, TValue> item in GetAll())
            {
                yield return item.Key;
            }
        }
    }

    public IEnumerable<TValue> Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            foreach (KeyValuePair<TKey, TValue> item in GetAll())
            {
                yield return item.Value;
            }
        }
    }

    /// <summary>
    /// Gets the number of items that are contained in the collection.
    /// Attention: This is can be expensive as determining the number of items requires a full iteration over all items.
    /// </summary>
    public long Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            object lockObject = new();
            long count = 0L;

            Enumerable.Range(0, DegreeOfParallelism).ToParallel(DegreeOfParallelism, i =>
            {
                lock (_connections[i])
                {
                    long subCount = (long)_countItemsCommands[i].ExecuteScalar();
                    lock (lockObject)
                    {
                        count += subCount;
                    }
                }
            });
            return count;
        }
    }

    TValue IReadOnlyLargeDictionary<TKey, TValue>.this[TKey key] => Get(key);

    public TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Get(key);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(key, value);
    }

    protected readonly Func<TKey, byte[]> _serializeKeyFunction;
    protected readonly Func<byte[], TKey> _deserializeKeyFunction;
    protected readonly Func<TValue, byte[]> _serializeValueFunction;
    protected readonly Func<byte[], TValue> _deserializeValueFunction;

    protected readonly SqliteType _keyType;
    protected readonly SqliteType _valueType;

    protected SqliteConnection[] _connections;

    protected SqliteTransaction[] _transactions;

    protected SqliteCommand[] _upsertItemCommands;
    protected SqliteParameter[] _upsertItemIdParameters;
    protected SqliteParameter[] _upsertItemItemParameters;

    protected SqliteCommand[] _deleteItemCommands;
    protected SqliteParameter[] _deleteItemIdParameters;

    protected SqliteCommand[] _queryIdAndItemCommands;
    protected SqliteParameter[] _queryIdAndItemIdParameters;

    protected SqliteCommand[] _queryItemCommands;
    protected SqliteParameter[] _queryItemIdParameters;

    protected SqliteCommand[] _countItemsCommands;

    protected SqliteCommand[] _clearItemsCommands;

    protected SqliteCommand[] _queryAllItemsCommands;

    public DiskCache(
        string baseFilePath,
        string fileExtension = DiskCacheConstants.DefaultFileExtension,
        byte degreeOfParallelism = DiskCacheConstants.DefaultDegreeOfParallelism,
        long maxMemorySize = DiskCacheConstants.DefaultMaxMemorySize,
        bool overwriteExistingFiles = true,
        bool deleteFilesOnDispose = true,
        bool isReadOnly = false,
        Func<TKey, byte[]> serializeKeyFunction = null,
        Func<byte[], TKey> deserializeKeyFunction = null,
        Func<TValue, byte[]> serializeValueFunction = null,
        Func<byte[], TValue> deserializeValueFunction = null
        )
    {
        if (degreeOfParallelism == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism));
        }
        if ((overwriteExistingFiles || deleteFilesOnDispose) && isReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        _serializeKeyFunction = serializeKeyFunction;
        _deserializeKeyFunction = deserializeKeyFunction;
        _serializeValueFunction = serializeValueFunction;
        _deserializeValueFunction = deserializeValueFunction;

        if (typeof(TKey) == typeof(long))
        {
            _keyType = SqliteType.Integer;
        }
        else if (typeof(TKey) == typeof(string))
        {
            _keyType = SqliteType.Text;
        }
        else if (typeof(TKey) == typeof(byte[]))
        {
            _keyType = SqliteType.Blob;
        }
        else
        {
            _keyType = SqliteType.Blob;

            if (_serializeKeyFunction is null)
            {
                throw new ArgumentException($"If TKey is none of long, string or byte[] a serializeKeyFunction must be provided.");
            }
            if (_deserializeKeyFunction is null)
            {
                throw new ArgumentException($"If TKey is none of long, string or byte[] a deserializeKeyFunction must be provided.");
            }
        }

        if (typeof(TValue) == typeof(long))
        {
            _valueType = SqliteType.Integer;
        }
        else if (typeof(TValue) == typeof(string))
        {
            _valueType = SqliteType.Text;
        }
        else if (typeof(TValue) == typeof(byte[]))
        {
            _valueType = SqliteType.Blob;
        }
        else if (typeof(TValue) == typeof(double))
        {
            _valueType = SqliteType.Real;
        }
        else
        {
            _valueType = SqliteType.Blob;

            if (_serializeValueFunction is null)
            {
                throw new ArgumentException($"If TValue is none of long, string, byte[] or double a serializeValueFunction must be provided.");
            }
            if (_deserializeValueFunction is null)
            {
                throw new ArgumentException($"If TValue is none of long, string, byte[] or double a deserializeValueFunction must be provided.");
            }
        }

        BaseFilePath = baseFilePath;
        FileExtension = fileExtension;

        MaxMemorySize = maxMemorySize >= 0L ? MaxMemorySize : DiskCacheConstants.DefaultMaxMemorySize;

        DegreeOfParallelism = degreeOfParallelism;

        OverwriteExistingFiles = overwriteExistingFiles;
        DeleteFilesOnDispose = deleteFilesOnDispose;
        IsReadOnly = isReadOnly;

        _connections = new SqliteConnection[DegreeOfParallelism];
        _transactions = new SqliteTransaction[DegreeOfParallelism];
        _upsertItemCommands = new SqliteCommand[DegreeOfParallelism];
        _upsertItemIdParameters = new SqliteParameter[DegreeOfParallelism];
        _upsertItemItemParameters = new SqliteParameter[DegreeOfParallelism];
        _deleteItemCommands = new SqliteCommand[DegreeOfParallelism];
        _deleteItemIdParameters = new SqliteParameter[DegreeOfParallelism];
        _queryIdAndItemCommands = new SqliteCommand[DegreeOfParallelism];
        _queryIdAndItemIdParameters = new SqliteParameter[DegreeOfParallelism];
        _queryItemCommands = new SqliteCommand[DegreeOfParallelism];
        _queryItemIdParameters = new SqliteParameter[DegreeOfParallelism];
        _countItemsCommands = new SqliteCommand[DegreeOfParallelism];
        _clearItemsCommands = new SqliteCommand[DegreeOfParallelism];
        _queryAllItemsCommands = new SqliteCommand[DegreeOfParallelism];

        Enumerable.Range(0, DegreeOfParallelism)
            .ToParallel(DegreeOfParallelism, i =>
            {
                string filePath = GetFilePath((byte)i);

                if (OverwriteExistingFiles)
                {
                    File.Delete(filePath);
                }

                SqliteConnectionStringBuilder connectionStringBuilder = new()
                {
                    DataSource = filePath,
                    Mode = IsReadOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                };

                string connectionString = connectionStringBuilder.ToString();

                _connections[i] = new SqliteConnection(connectionString);
                _connections[i].Open();

                using SqliteCommand command = _connections[i].CreateCommand();

                command.CommandText = $"PRAGMA page_size = {DiskCacheConstants.PageSize};";
                command.ExecuteNonQuery();

                long cacheSize = MaxMemorySize * 1000L * 1000L / ((long)DegreeOfParallelism * DiskCacheConstants.PageSize);
                command.CommandText = $"PRAGMA cache_size = {cacheSize};";
                command.ExecuteNonQuery();

                command.CommandText = "PRAGMA journal_mode = OFF;";
                command.ExecuteNonQuery();

                command.CommandText = "PRAGMA synchronous = OFF;";
                command.ExecuteNonQuery();

            });

        RecreateTable(OverwriteExistingFiles);

        Enumerable.Range(0, DegreeOfParallelism).ToParallel(DegreeOfParallelism, i =>
        {
            _transactions[i] = IsReadOnly ?
                _connections[i].BeginTransaction(System.Data.IsolationLevel.ReadUncommitted) :
                _connections[i].BeginTransaction();

            _upsertItemCommands[i] = _connections[i].CreateCommand();
            _upsertItemCommands[i].CommandText = $"INSERT INTO items (id, item) VALUES (@id, @item) ON CONFLICT(id) DO UPDATE SET item = @item;";
            _upsertItemIdParameters[i] = new SqliteParameter("@id", _keyType);
            _upsertItemItemParameters[i] = new SqliteParameter("@item", _valueType);
            _upsertItemCommands[i].Parameters.Add(_upsertItemIdParameters[i]);
            _upsertItemCommands[i].Parameters.Add(_upsertItemItemParameters[i]);
            _upsertItemCommands[i].Transaction = _transactions[i];
            _upsertItemCommands[i].Prepare();

            _deleteItemCommands[i] = _connections[i].CreateCommand();
            _deleteItemCommands[i].CommandText = $"DELETE FROM items WHERE id = @id RETURNING item;";
            _deleteItemIdParameters[i] = new SqliteParameter("@id", _keyType);
            _deleteItemCommands[i].Parameters.Add(_deleteItemIdParameters[i]);
            _deleteItemCommands[i].Transaction = _transactions[i];
            _deleteItemCommands[i].Prepare();

            _queryIdAndItemCommands[i] = _connections[i].CreateCommand();
            _queryIdAndItemCommands[i].CommandText = $"SELECT id, item FROM items WHERE id = @id;";
            _queryIdAndItemIdParameters[i] = new SqliteParameter("@id", _keyType);
            _queryIdAndItemCommands[i].Parameters.Add(_queryIdAndItemIdParameters[i]);
            _queryIdAndItemCommands[i].Transaction = _transactions[i];
            _queryIdAndItemCommands[i].Prepare();

            _queryItemCommands[i] = _connections[i].CreateCommand();
            _queryItemCommands[i].CommandText = $"SELECT item FROM items WHERE id = @id;";
            _queryItemIdParameters[i] = new SqliteParameter("@id", _keyType);
            _queryItemCommands[i].Parameters.Add(_queryItemIdParameters[i]);
            _queryItemCommands[i].Transaction = _transactions[i];
            _queryItemCommands[i].Prepare();

            _countItemsCommands[i] = _connections[i].CreateCommand();
            _countItemsCommands[i].CommandText = $"SELECT COUNT(id) FROM items;";
            _countItemsCommands[i].Transaction = _transactions[i];
            _countItemsCommands[i].Prepare();

            _clearItemsCommands[i] = _connections[i].CreateCommand();
            _clearItemsCommands[i].CommandText = $"DELETE FROM items;";
            _clearItemsCommands[i].Transaction = _transactions[i];
            _clearItemsCommands[i].Prepare();

            _queryAllItemsCommands[i] = _connections[i].CreateCommand();
            _queryAllItemsCommands[i].CommandText = $"SELECT id, item FROM items;";
            _queryAllItemsCommands[i].Transaction = _transactions[i];
            _queryAllItemsCommands[i].Prepare();
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RecreateTable(bool overwriteExistingFiles)
    {
        Enumerable.Range(0, DegreeOfParallelism).ToParallel(DegreeOfParallelism, i =>
        {
            using SqliteCommand command = _connections[i].CreateCommand();

            if (overwriteExistingFiles)
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
                }

                command.CommandText = $"DROP TABLE IF EXISTS items;";
                command.ExecuteNonQuery();
            }
            string keyType = _keyType.ToString().ToUpper();
            string valueType = _valueType.ToString().ToUpper();

            command.CommandText = $"CREATE TABLE IF NOT EXISTS items (id {keyType} NOT NULL PRIMARY KEY, item {valueType});";
            command.ExecuteNonQuery();

        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(TKey key, TValue value)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        int index = 0;
        object keyParameterValue = null;

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (typeof(TKey) == typeof(long))
        {
            if (key is long longKey)
            {
                index = GetParallelIndex(longKey, DegreeOfParallelism);
                keyParameterValue = longKey;
            }
        }
        else if (typeof(TKey) == typeof(string))
        {
            if (key is string stringKey)
            {
                if (stringKey.Length == 0)
                {
                    throw new ArgumentException("The length of the key must not be zero.", nameof(key));
                }

                index = GetParallelIndex(stringKey, DegreeOfParallelism);
                keyParameterValue = stringKey;
            }
        }
        else if (typeof(TKey) == typeof(byte[]))
        {
            if (key is byte[] bytesKey)
            {
                if (bytesKey.Length == 0)
                {
                    throw new ArgumentException("The length of the key must not be zero.", nameof(key));
                }
                if (bytesKey.Length > DiskCacheConstants.MaxItemLength)
                {
                    throw new Exception($"The length of the key must not exceed {DiskCacheConstants.MaxItemLength} byte.");
                }

                index = GetParallelIndex(bytesKey, DegreeOfParallelism);
                keyParameterValue = bytesKey;
            }
        }
        else
        {
            if (_serializeKeyFunction is null)
            {
                return;
            }

            byte[] serializedKey = _serializeKeyFunction.Invoke(key)
                ?? throw new InvalidOperationException("The serialized key must not be null.");

            if (serializedKey.Length == 0)
            {
                throw new ArgumentException("The length of the serialized key must not be zero.");
            }
            if (serializedKey.Length > DiskCacheConstants.MaxItemLength)
            {
                throw new Exception($"The length of the key must not exceed {DiskCacheConstants.MaxItemLength} byte.");
            }

            index = GetParallelIndex(serializedKey, DegreeOfParallelism);
            keyParameterValue = serializedKey;
        }

        lock (_connections[index])
        {
            _upsertItemIdParameters[index].Value = keyParameterValue;
            if (typeof(TValue) == typeof(long) || typeof(TValue) == typeof(string) || typeof(TValue) == typeof(byte[]) || typeof(TValue) == typeof(double))
            {
                if (value is byte[] bytesValue && bytesValue is not null && bytesValue.Length > DiskCacheConstants.MaxItemLength)
                {
                    throw new Exception($"The length of the item must not exceed {DiskCacheConstants.MaxItemLength} byte.");
                }

                _upsertItemItemParameters[index].Value = value;
            }
            else
            {
                byte[] serializedValue = _serializeValueFunction(value);

                if (serializedValue is not null && serializedValue.Length > DiskCacheConstants.MaxItemLength)
                {
                    throw new Exception($"The length of the item must not exceed {DiskCacheConstants.MaxItemLength} byte.");
                }

                _upsertItemItemParameters[index].Value = serializedValue;
            }

            _upsertItemCommands[index].ExecuteNonQuery();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Get(TKey key)
    {
        if (!TryGetValue(key, out TValue value))
        {
            throw new KeyNotFoundException(key.ToString());
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        value = default;

        int index = 0;
        object keyParameterValue = null;

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (typeof(TKey) == typeof(long))
        {
            if (key is long longKey)
            {
                index = GetParallelIndex(longKey, DegreeOfParallelism);
                keyParameterValue = longKey;
            }
        }
        else if (typeof(TKey) == typeof(string))
        {
            if (key is string stringKey)
            {
                if (stringKey.Length == 0)
                {
                    throw new ArgumentException("The length of the key must not be zero.", nameof(key));
                }

                index = GetParallelIndex(stringKey, DegreeOfParallelism);
                keyParameterValue = stringKey;
            }
        }
        else if (typeof(TKey) == typeof(byte[]))
        {
            if (key is byte[] bytesKey)
            {
                if (bytesKey.Length == 0)
                {
                    throw new ArgumentException("The length of the key must not be zero.", nameof(key));
                }
                if (bytesKey.Length > DiskCacheConstants.MaxItemLength)
                {
                    throw new Exception($"The length of the key must not exceed {DiskCacheConstants.MaxItemLength} byte.");
                }

                index = GetParallelIndex(bytesKey, DegreeOfParallelism);
                keyParameterValue = bytesKey;
            }
        }
        else
        {
            if (_serializeKeyFunction is null)
            {
                return false;
            }

            byte[] serializedKey = _serializeKeyFunction(key)
                ?? throw new InvalidOperationException("The serialized key must not be null.");

            if (serializedKey.Length == 0)
            {
                throw new ArgumentException("The length of the serialized key must not be zero.", nameof(key));
            }
            if (serializedKey.Length > DiskCacheConstants.MaxItemLength)
            {
                throw new Exception($"The length of the key must not exceed {DiskCacheConstants.MaxItemLength} byte.");
            }

            index = GetParallelIndex(serializedKey, DegreeOfParallelism);
            keyParameterValue = serializedKey;
        }

        lock (_connections[index])
        {
            _queryItemIdParameters[index].Value = keyParameterValue;

            object item = _queryItemCommands[index].ExecuteScalar();

            if (item != null)
            {
                if (typeof(TValue) == typeof(long) || typeof(TValue) == typeof(string) || typeof(TValue) == typeof(byte[]) || typeof(TValue) == typeof(double))
                {
                    value = (TValue)item;
                }
                else
                {
                    byte[] serializedValue = (byte[])item;

                    value = _deserializeValueFunction(serializedValue);
                }

                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        Set(item.Key, item.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        foreach (KeyValuePair<TKey, TValue> item in items)
        {
            Set(item.Key, item.Value);
        }
    }

    public void AddRange(ReadOnlyLargeSpan<KeyValuePair<TKey, TValue>> items)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        for (long i = 0L; i < items.Count; i++)
        {
            KeyValuePair<TKey, TValue> item = items[i];
            Set(item.Key, item.Value);
        }
    }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER || NET5_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        for(int i = 0; i < items.Length; i++)
        {
            KeyValuePair<TKey, TValue> item = items[i];
            Set(item.Key, item.Value);
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddParallel(IEnumerable<KeyValuePair<TKey, TValue>>[] parallelItems)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        parallelItems.DoParallel(item => Set(item.Key, item.Value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
        => Remove(key, true, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key, out TValue removedValue)
    => Remove(key, false, out removedValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item)
        => Remove(item.Key, false, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item, out TValue removedValue)
        => Remove(item.Key, false, out removedValue);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TKey, TValue> item, out KeyValuePair<TKey, TValue> removedItem)
    {
        removedItem = default;

        if (Remove(item.Key, false, out TValue removedValue))
        {
            removedItem = new KeyValuePair<TKey, TValue>(item.Key, removedValue);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Remove(TKey key, bool ignoreReturnedValue, out TValue removedValue)
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        removedValue = default;

        int index = 0;
        object keyParameterValue = null;

        if (typeof(TKey) == typeof(long))
        {
            if (key is long longKey)
            {
                index = GetParallelIndex(longKey, DegreeOfParallelism);
                keyParameterValue = longKey;
            }
        }
        else if (typeof(TKey) == typeof(string))
        {
            if (key is string stringKey)
            {
                if (stringKey.Length == 0)
                {
                    return false;
                }

                index = GetParallelIndex(stringKey, DegreeOfParallelism);
                keyParameterValue = stringKey;
            }
        }
        else if (typeof(TKey) == typeof(byte[]))
        {
            if (key is byte[] bytesKey)
            {
                if (bytesKey.Length == 0)
                {
                    return false;
                }
                if (bytesKey.Length > DiskCacheConstants.MaxItemLength)
                {
                    throw new Exception($"The length of the key must not exceed {DiskCacheConstants.MaxItemLength} byte.");
                }

                index = GetParallelIndex(bytesKey, DegreeOfParallelism);
                keyParameterValue = bytesKey;
            }
        }
        else
        {
            if (_serializeKeyFunction is null)
            {
                return false;
            }

            byte[] serializedKey = _serializeKeyFunction(key)
                ?? throw new InvalidOperationException("The serialized key must not be null.");

            if (serializedKey.Length == 0)
            {
                throw new ArgumentException("The serialized key must not be empty.", nameof(key));
            }
            if (serializedKey.Length > DiskCacheConstants.MaxItemLength)
            {
                throw new Exception($"The length of the key must not exceed {DiskCacheConstants.MaxItemLength} byte.");
            }

            index = GetParallelIndex(serializedKey, DegreeOfParallelism);
            keyParameterValue = serializedKey;
        }

        lock (_connections[index])
        {
            _deleteItemIdParameters[index].Value = keyParameterValue;

            object removedItem = _deleteItemCommands[index].ExecuteScalar();

            if (removedItem is null)
            {
                return false;
            }
            if (ignoreReturnedValue)
            {
                return true;
            }

            if (typeof(TValue) == typeof(long) || typeof(TValue) == typeof(string) || typeof(TValue) == typeof(byte[]) || typeof(TValue) == typeof(double))
            {
                removedValue = (TValue)removedItem;
            }
            else
            {
                byte[] serializedValue = (byte[])removedItem;

                removedValue = _deserializeValueFunction(serializedValue);
            }
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveParallel(IEnumerable<TKey>[] parallelKeys)
    {
        if (parallelKeys is null)
        {
            throw new ArgumentNullException(nameof(parallelKeys));
        }

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");

        }
        parallelKeys.DoParallel(key => Remove(key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveParallel(IEnumerable<KeyValuePair<TKey, TValue>>[] parallelItems)
    {
        if (parallelItems is null)
        {
            throw new ArgumentNullException(nameof(parallelItems));
        }

        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        parallelItems.DoParallel(item => Remove(item.Key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Clear()
    {
        if (IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        Enumerable.Range(0, DegreeOfParallelism).ToParallel(DegreeOfParallelism, i =>
        {
            lock (_connections[i])
            {
                _clearItemsCommands[i].ExecuteNonQuery();
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return TryGetValue(item.Key, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IEnumerable<KeyValuePair<TKey, TValue>> GetAllInternal(SqliteConnection connection, SqliteCommand queryAllItemsCommand)
    {
        if (connection is null)
        {
            yield break;
        }
        if (queryAllItemsCommand is null)
        {
            yield break;
        }

        lock (connection)
        {
            using SqliteDataReader reader = queryAllItemsCommand.ExecuteReader();

            while (reader.Read())
            {
                TKey key = default;
                if (typeof(TKey) == typeof(long) || typeof(TKey) == typeof(string) || typeof(TKey) == typeof(byte[]))
                {
                    key = (TKey)reader.GetValue(0);
                }
                else
                {
                    byte[] serializedKey = (byte[])reader.GetValue(0);

                    key = _deserializeKeyFunction(serializedKey);
                }

                TValue value = default;
                if (typeof(TValue) == typeof(long) || typeof(TValue) == typeof(string) || typeof(TValue) == typeof(byte[]) || typeof(TValue) == typeof(double))
                {
                    value = (TValue)reader.GetValue(1);
                }
                else
                {
                    byte[] serializedValue = (byte[])reader.GetValue(1);

                    value = _deserializeValueFunction(serializedValue);
                }

                yield return new KeyValuePair<TKey, TValue>(key, value);
            }

        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<KeyValuePair<TKey, TValue>> GetAll()
    {
        for (byte i = 0; i < DegreeOfParallelism; i++)
        {
            foreach (KeyValuePair<TKey, TValue> item in GetAllInternal(_connections[i], _queryAllItemsCommands[i]))
            {
                yield return item;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<KeyValuePair<TKey, TValue>>[] GetAllParallel()
    {
        IEnumerable<KeyValuePair<TKey, TValue>>[] parallelItems = new IEnumerable<KeyValuePair<TKey, TValue>>[DegreeOfParallelism];
        for (int i = 0; i < DegreeOfParallelism; i++)
        {
            parallelItems[i] = GetAllInternal(_connections[i], _queryAllItemsCommands[i]);
        }

        return parallelItems;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetAll().GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Dispose()
    {
        if (DeleteFilesOnDispose && IsReadOnly)
        {
            throw new InvalidOperationException("Modifications are not allowed when opened in read only mode.");
        }

        if (_connections is null)
        {
            return;
        }

        if (DeleteFilesOnDispose)
        {
            Clear();
        }

        for (int i = 0; i < DegreeOfParallelism; i++)
        {
            lock (_connections[i])
            {
                string filePath = GetFilePath((byte)i);

                if (_upsertItemCommands is not null && _upsertItemCommands[i] is not null)
                {
                    _upsertItemCommands[i].Dispose();
                    _upsertItemCommands[i] = null;
                }

                if (_deleteItemCommands is not null && _deleteItemCommands[i] is not null)
                {
                    _deleteItemCommands[i].Dispose();
                    _deleteItemCommands[i] = null;
                }

                if (_queryIdAndItemCommands is not null && _queryIdAndItemCommands[i] is not null)
                {
                    _queryIdAndItemCommands[i].Dispose();
                    _queryIdAndItemCommands[i] = null;
                }

                if (_queryItemCommands is not null && _queryItemCommands[i] is not null)
                {
                    _queryItemCommands[i].Dispose();
                    _queryItemCommands[i] = null;
                }

                if (_countItemsCommands is not null && _countItemsCommands[i] is not null)
                {
                    _countItemsCommands[i].Dispose();
                    _countItemsCommands[i] = null;
                }

                if (_clearItemsCommands is not null && _clearItemsCommands[i] is not null)
                {
                    _clearItemsCommands[i].Dispose();
                    _clearItemsCommands[i] = null;
                }

                if (_queryAllItemsCommands is not null && _queryAllItemsCommands[i] is not null)
                {
                    _queryAllItemsCommands[i].Dispose();
                    _queryAllItemsCommands[i] = null;
                }


                if (_transactions is not null && _transactions[i] is not null)
                {
                    if (!DeleteFilesOnDispose)
                    {
                        _transactions[i].Commit();
                    }

                    _transactions[i].Dispose();
                    _transactions[i] = null;

                }

                if (_connections is not null && _connections[i] is not null)
                {
                    _connections[i].Close();
                    _connections[i].Dispose();
                    _connections[i] = null;
                }

                if (DeleteFilesOnDispose)
                {
                    File.Delete(filePath);
                }
            }
        }

        _upsertItemCommands = null;
        _deleteItemCommands = null;
        _queryIdAndItemCommands = null;
        _queryItemCommands = null;
        _countItemsCommands = null;
        _clearItemsCommands = null;
        _queryAllItemsCommands = null;
        _transactions = null;
        _connections = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int GetParallelIndex(long longId, byte degreeOfParallelism)
    {
        if (degreeOfParallelism <= 1)
        {
            return 0;
        }

        int index = 0;

        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);
        longId >>= 8;
        index ^= (int)(longId & 0xFFL);

        if (degreeOfParallelism <= 16)
        {
            index ^= (index & 0xF0) >> 4;
        }

        index = (byte)index % degreeOfParallelism;

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int GetParallelIndex(string stringId, byte degreeOfParallelism)
    {
        if (degreeOfParallelism <= 1)
        {
            return 0;
        }

        int index = 0;

        if (stringId is null)
        {
            return 0;
        }

        for (int i = 0; i < stringId.Length; i++)
        {
            int currentChar = (int)stringId[i];
            index ^= currentChar & 0xFF;
            currentChar >>= 8;
            index ^= currentChar & 0xFF;
        }

        if (degreeOfParallelism <= 16)
        {
            index ^= (index & 0xF0) >> 4;
        }

        index = (byte)index % degreeOfParallelism;

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int GetParallelIndex(byte[] bytesId, byte degreeOfParallelism)
    {
        if (degreeOfParallelism <= 1)
        {
            return 0;
        }

        int index = 0;

        if (bytesId is null)
        {
            return 0;
        }

        for (int i = 0; i < bytesId.Length; i++)
        {
            index ^= bytesId[i];
        }

        if (degreeOfParallelism <= 16)
        {
            index ^= (index & 0xF0) >> 4;
        }

        index = (byte)index % degreeOfParallelism;

        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach(Action<KeyValuePair<TKey, TValue>> action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        foreach (KeyValuePair<TKey, TValue> item in GetAll())
        {
            action.Invoke(item);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoForEach<TUserData>(ActionWithUserData<KeyValuePair<TKey, TValue>, TUserData> action, ref TUserData userData)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        foreach (KeyValuePair<TKey, TValue> item in GetAll())
        {
            action.Invoke(item, ref userData);
        }
    }
}
