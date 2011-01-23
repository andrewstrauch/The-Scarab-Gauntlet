//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using GarageGames.Torque.Core;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Utility object which can be used to perform repeated pattern matching
    /// against the same pattern with different strings.
    /// </summary>
    public struct PatternMatch
    {
        #region Constructors

        /// <summary>
        /// Create a new pattern match struct.
        /// </summary>
        /// <param name="pattern">Pattern to search for.  The '*' and '?' characters are interpreted as wildcard symbols
        /// (no escape sequence recognized).  The '*' symbol matches any substring while the '?' 
        /// character matches any single character.</param>
        public PatternMatch(string pattern) : this(pattern, false) { }

        /// <summary>
        /// Create a new pattern match struct.
        /// </summary>
        /// <param name="pattern">Pattern to search for.  The '*' and '?' characters are interpreted as wildcard symbols
        /// (no escape sequence recognized).  The '*' symbol matches any substring while the '?' 
        /// character matches any single character.</param>
        /// <param name="caseSensitive">Set to true if you want a case-sensitive search. False by default.</param>
        public PatternMatch(string pattern, bool caseSensitive)
        {
            _pattern = pattern;
            _hasWildcard = (_pattern.IndexOf('*') >= 0 || _pattern.IndexOf('?') >= 0);
            _isCaseSensitive = caseSensitive;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Pattern to search for.  The '*' and '?' characters are interpreted as wildcard symbols
        /// (no escape sequence recognized).  The '*' symbol matches any substring while the '?' 
        /// character matches any single character.
        /// </summary>
        public string Pattern
        {
            get { return _pattern; }
            set { _pattern = value; _hasWildcard = (_pattern.IndexOf('*') >= 0 || _pattern.IndexOf('?') >= 0); }
        }

        /// <summary>
        /// True if Pattern contains a wildcard character.
        /// </summary>
        public bool HasWildcard
        {
            get { return _hasWildcard; }
        }

        /// <summary>
        /// Set to true if you want a case-sensitive search.  False by default.
        /// </summary>
        public bool IsCaseSensitive
        {
            get { return _isCaseSensitive; }
            set { _isCaseSensitive = value; }
        }

        /// <summary>
        /// Static pattern which matches all strings ("*").
        /// </summary>
        static public string MatchAll
        {
            get { return "*"; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Test string against pattern.
        /// </summary>
        /// <param name="str">String to test against pattern.</param>
        /// <returns>True if pattern matches string.</returns>
        public bool TestMatch(string str)
        {
            if (HasWildcard)
                return _TestMatch(str, _isCaseSensitive);
            else
                return string.Compare(str, _pattern, !_isCaseSensitive) == 0;
        }

        #endregion


        #region Private, protected, internal methods

        bool _TestMatch(string str, bool caseSensitive)
        {
            Assert.Fatal(_pattern != null, "PatternMath._TestMatch - Must set pattern before using");
            Assert.Fatal(str != null, "PatternMath._TestMatch - Null string not handled");

            int strIdx = 0;
            int patIdx = 0;
            int strLen = str.Length;
            int patLen = _pattern.Length;

            int patBackup = -1;
            int strBackup = -1;

            while (strIdx < strLen && patIdx < patLen)
            {
                if (_pattern[patIdx] == '*')
                {
                    patIdx++;
                    if (patIdx == patLen)
                        return true;
                    patBackup = patIdx;
                    strBackup = strIdx;
                }
                else if (_pattern[patIdx] == str[strIdx] || _pattern[patIdx] == '?' || (!caseSensitive && (char.ToLower(_pattern[patIdx]) == char.ToLower(str[strIdx]))))
                {
                    patIdx++;
                    strIdx++;
                }
                else if (patBackup != -1)
                {
                    patIdx = patBackup;
                    strIdx = ++strBackup;
                }
                else
                    return false;
            }

            while (patIdx < patLen && _pattern[patIdx] == '*')
            {
                patIdx++;
                strIdx = strLen;
            }
            return patIdx == patLen && strIdx == strLen;
        }

        #endregion


        #region Private, protected, internal fields

        string _pattern;
        bool _hasWildcard;
        bool _isCaseSensitive;

        #endregion
    }
}
