using System;
using System.Security.Cryptography;

[Serializable]
public struct SecureInt : IEquatable<SecureInt>
{
    private const int NotInitialized = 0;

    private int _cryptoKey;
    private int _encryptedValue;
    private int _integrity;
    private int _initialized;

    public SecureInt(int value)
    {
        _cryptoKey = GenerateKey();
        _encryptedValue = value ^ _cryptoKey;
        _integrity = ComputeIntegrity(value, _cryptoKey);
        _initialized = 1;
    }

    public int Value
    {
        get
        {
            if (_initialized == NotInitialized)
                return 0;

            return _encryptedValue ^ _cryptoKey;
        }
    }

    public bool IsValid
    {
        get
        {
            if (_initialized == NotInitialized)
                return true;

            int value = _encryptedValue ^ _cryptoKey;
            return _integrity == ComputeIntegrity(value, _cryptoKey);
        }
    }

    public bool TryGetValue(out int value)
    {
        value = Value;
        return IsValid;
    }

    public void SetValue(int value)
    {
        if (_initialized == NotInitialized)
        {
            _cryptoKey = GenerateKey();
            _initialized = 1;
        }

        _encryptedValue = value ^ _cryptoKey;
        _integrity = ComputeIntegrity(value, _cryptoKey);
    }

    public void Rekey()
    {
        if (_initialized == NotInitialized)
        {
            _cryptoKey = GenerateKey();
            _integrity = ComputeIntegrity(0, _cryptoKey);
            _initialized = 1;
            return;
        }

        int value = _encryptedValue ^ _cryptoKey;
        _cryptoKey = GenerateKey();
        _encryptedValue = value ^ _cryptoKey;
        _integrity = ComputeIntegrity(value, _cryptoKey);
    }

    public static implicit operator int(SecureInt secure) => secure.Value;
    public static implicit operator SecureInt(int value) => new SecureInt(value);

    public bool Equals(SecureInt other) => Value == other.Value;
    public override bool Equals(object obj) => obj is SecureInt other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    private static int GenerateKey()
    {
        int key;
        var bytes = new byte[4];
        do
        {
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            key = BitConverter.ToInt32(bytes, 0);
        } while (key == 0);

        return key;
    }

    private static int ComputeIntegrity(int value, int key)
    {
        unchecked
        {
            uint x = (uint)value;
            uint k = (uint)key;
            uint h = 0x9E3779B9u;
            h ^= x + 0x7F4A7C15u + (h << 6) + (h >> 2);
            h ^= k + 0x94D049BBu + (h << 6) + (h >> 2);
            return (int)h;
        }
    }
}