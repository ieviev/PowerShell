// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System.Management.Automation.Internal
{
    
    public class StringDecorated
    {
        private readonly bool _isDecorated;
        private readonly string _text;
        private string? _plaintextcontent;

        private string PlainText
        {
            get
            {
                _plaintextcontent ??= ValueStringDecorated.AnsiRegex.Replace(_text, string.Empty);

                return _plaintextcontent;
            }
        }

        
        public StringDecorated(string text)
        {
            _text = text;
            _isDecorated = text.Contains(ValueStringDecorated.ESC);
        }

        
        public bool IsDecorated => _isDecorated;

        
        public int ContentLength => PlainText.Length;

        
        public override string ToString() => ToString(
            PSStyle.Instance.OutputRendering == OutputRendering.PlainText
                ? OutputRendering.PlainText
                : OutputRendering.Ansi);

        
        public string ToString(OutputRendering outputRendering)
        {
            if (outputRendering == OutputRendering.Host)
            {
                throw new ArgumentException(StringDecoratedStrings.RequireExplicitRendering);
            }

            if (!_isDecorated)
            {
                return _text;
            }

            return outputRendering == OutputRendering.PlainText ? PlainText : _text;
        }
    }

    internal struct ValueStringDecorated
    {
        internal const char ESC = '\x1b';
        private readonly bool _isDecorated;
        private readonly string _text;
        private string? _plaintextcontent;
        private Dictionary<int, int>? _vtRanges;

        private string PlainText
        {
            get
            {
                _plaintextcontent ??= AnsiRegex.Replace(_text, string.Empty);

                return _plaintextcontent;
            }
        }

        // graphics/color mode ESC[1;2;...m
        private const string GraphicsRegex = @"(\x1b\[\d*(;\d+)*m)";

        // CSI escape sequences
        private const string CsiRegex = @"(\x1b\[\?\d+[hl])";

        // Hyperlink escape sequences. Note: '.*?' makes '.*' do non-greedy match.
        private const string HyperlinkRegex = @"(\x1b\]8;;.*?\x1b\\)";

        // replace regex with .NET 6 API once available
        internal static readonly Regex AnsiRegex = new Regex($"{GraphicsRegex}|{CsiRegex}|{HyperlinkRegex}", RegexOptions.Compiled);

        
        internal Dictionary<int, int>? EscapeSequenceRanges
        {
            get
            {
                if (_isDecorated && _vtRanges is null)
                {
                    _vtRanges = new Dictionary<int, int>();
                    foreach (Match match in AnsiRegex.Matches(_text))
                    {
                        _vtRanges.Add(match.Index, match.Length);
                    }
                }

                return _vtRanges;
            }
        }

        
        public ValueStringDecorated(string text)
        {
            _text = text;
            _isDecorated = text.Contains(ESC);
            _plaintextcontent = _isDecorated ? null : text;
            _vtRanges = null;
        }

        
        public bool IsDecorated => _isDecorated;

        
        public int ContentLength => PlainText.Length;

        
        public override string ToString() => ToString(
            PSStyle.Instance.OutputRendering == OutputRendering.PlainText
                ? OutputRendering.PlainText
                : OutputRendering.Ansi);

        
        public string ToString(OutputRendering outputRendering)
        {
            if (outputRendering == OutputRendering.Host)
            {
                throw new ArgumentException(StringDecoratedStrings.RequireExplicitRendering);
            }

            if (!_isDecorated)
            {
                return _text;
            }

            return outputRendering == OutputRendering.PlainText ? PlainText : _text;
        }
    }
}
