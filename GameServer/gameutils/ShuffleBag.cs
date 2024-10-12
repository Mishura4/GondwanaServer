
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using System.Collections.Generic;
using System.Drawing;

namespace DOL.GS.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public class ShuffleBag
    {
        private readonly BitArray _values;

        private readonly Random _random;

        private readonly int _numHits;

        private int _cur;

        public ShuffleBag(int numHits, int totalDraws, Random rng)
        {
            _numHits = numHits;
            _random = rng;
            if (numHits == 0)
            {
                _values = new BitArray(1, false);
                return;
            }
            if (numHits >= totalDraws)
            {
                _values = new BitArray(1, true);
                return;
            }
            _values = new BitArray(totalDraws);
            for (int i = 0; i < numHits; ++i)
            {
                _values[i] = true;
            }
            Shuffle();
            _cur = 0;
        }

        public int Size => _values.Length;

        public int NumHits => _numHits;

        public int Cursor
        {
            get
            {
                lock (_values)
                {
                    return _cur;
                }
            }
        }

        public bool[] CurrentValues
        {
            get
            {
                lock (_values)
                {
                    return _values.Cast<bool>().ToArray();
                }
            }
        }

        public static (int hits, int draws) CalculateDimensions(int chance, int multiplier)
        {
            int gcd = GameMath.GCD(Math.Clamp(chance, 0, 100), 100);
            if (multiplier < 1)
                multiplier = 1;
            int numHits = chance / gcd * multiplier;
            int total = 100 / gcd * multiplier;

            return (numHits, total);
        }
        
        private void Shuffle()
        {
            int n = _values.Length;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n);
                bool temp = _values[n];
                _values[n] = _values[k];
                _values[k] = temp;
            }
        }

        public bool Next()
        {
            lock (_values)
            {
                if (_cur >= _values.Length)
                {
                    Shuffle();
                    _cur = 0;
                }
                bool ret = _values[_cur];
                ++_cur;
                return ret;
            }
        }

        public override string ToString()
        {
            lock (_values)
            {
                string valString = string.Empty;
                
                for (int i = 0; i < _cur; ++i)
                {
                    valString += (_values[i] ? 1 : 0);
                }
                valString += '|';
                for (int i = _cur; i < _values.Length; ++i)
                {
                    valString += (_values[i] ? 1 : 0);
                }
                return $"{{Chance: {_numHits}/{_values.Length} ({_numHits/_values.Length:F}%), Values: [{_values.Length}]{{{valString}}}}}";
            }
        }
    }
}
