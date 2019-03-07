using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoCameo.Server.Business
{
  public enum TokenType
  {
    NotDefined,
    And,
    Application,
    Between,
    CloseParenthesis,
    Comma,
    DateTimeValue,
    Equals,
    ExceptionType,
    Fingerprint,
    In,
    Invalid,
    Like,
    Limit,
    Match,
    Message,
    NotEquals,
    NotIn,
    NotLike,
    Number,
    Or,
    OpenParenthesis,
    StackFrame,
    StringValue,
    SequenceTerminator
  }
  public class Tokenizer
  {
    List<TokenDefinition> _tokenDefinitions;
    public Tokenizer()
    {
      _tokenDefinitions = new List<TokenDefinition>();

      _tokenDefinitions.Add(new TokenDefinition(TokenType.And, "^and"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Application, "^app|^application"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Between, "^between"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.CloseParenthesis, "^\\)"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Comma, "^,"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Equals, "^="));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.ExceptionType, "^ex|^exception"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Fingerprint, "^fingerprint"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.NotIn, "^not in"));
      // _tokenDefinitions.Add(new TokenDefinition(TokenType.In, "^in"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Like, "^like"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Limit, "^limit"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Match, "^match"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Message, "^msg|^message"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.NotEquals, "^!="));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.NotLike, "^not like"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.OpenParenthesis, "^\\("));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.Or, "^or"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.StackFrame, "^sf|^stackframe"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.DateTimeValue, "^\\d\\d\\d\\d-\\d\\d-\\d\\d \\d\\d:\\d\\d:\\d\\d"));
      _tokenDefinitions.Add(new TokenDefinition(TokenType.StringValue, "^'[^']*'"));
      //_tokenDefinitions.Add(new TokenDefinition(TokenType.Number, "^\\d+"));
    }
    public List<DslToken> Tokenize(string lqlText)
    {
      var tokens = new List<DslToken>();
      string remainingText = lqlText;

      while (!string.IsNullOrWhiteSpace(remainingText))
      {
        var match = FindMatch(remainingText);
        if (match.IsMatch)
        {
          tokens.Add(new DslToken(match.TokenType, match.Value));
          remainingText = match.RemainingText;
        }
        else
        {
          remainingText = remainingText.Substring(1);
        }
      }

      tokens.Add(new DslToken(TokenType.SequenceTerminator, string.Empty));

      return tokens;
    }

    private TokenMatch FindMatch(string lqlText)
    {
      foreach (var tokenDefinition in _tokenDefinitions)
      {
        var match = tokenDefinition.Match(lqlText);
        if (match.IsMatch)
          return match;
      }

      return new TokenMatch() { IsMatch = false };
    }

    private bool IsWhitespace(string lqlText)
    {
      return Regex.IsMatch(lqlText, "^\\s+");
    }

    //private TokenMatch CreateInvalidTokenMatch(string lqlText)
    //{
    //  var match = Regex.Match(lqlText, "(^\\S+\\s)|^\\S+");
    //  if (match.Success)
    //  {
    //    return new TokenMatch()
    //    {
    //      IsMatch = true,
    //      RemainingText = lqlText.Substring(match.Length),
    //      TokenType = TokenType.Invalid,
    //      Value = match.Value.Trim()
    //    };
    //  }

    //  // throw new DslParserException("Failed to generate invalid token");
    //}
  }
  public class TokenDefinition
  {
    private Regex _regex;
    private readonly TokenType _returnsToken;

    public TokenDefinition(TokenType returnsToken, string regexPattern)
    {
      _regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
      _returnsToken = returnsToken;
    }

    public TokenMatch Match(string inputString)
    {
      var match = _regex.Match(inputString);
      if (match.Success)
      {
        string remainingText = string.Empty;
        if (match.Length != inputString.Length)
          remainingText = inputString.Substring(match.Length);

        return new TokenMatch()
        {
          IsMatch = true,
          RemainingText = remainingText,
          TokenType = _returnsToken,
          Value = match.Value
        };
      }
      else
      {
        return new TokenMatch() { IsMatch = false };
      }

    }
  }

  public class TokenMatch
  {
    public bool IsMatch { get; set; }
    public TokenType TokenType { get; set; }
    public string Value { get; set; }
    public string RemainingText { get; set; }
  }

  public class DslToken
  {
    public DslToken(TokenType tokenType)
    {
      TokenType = tokenType;
      Value = string.Empty;
    }

    public DslToken(TokenType tokenType, string value)
    {
      TokenType = tokenType;
      Value = value;
    }

    public TokenType TokenType { get; set; }
    public string Value { get; set; }

    public DslToken Clone()
    {
      return new DslToken(TokenType, Value);
    }
  }
}
