///////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2023 Laszlo Arvai. All rights reserved.
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation; either version 2.1 of the License, 
// or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
// MA 02110-1301  USA
///////////////////////////////////////////////////////////////////////////////
// File description
// ----------------
// Integer expression parser and evaluator
///////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;

namespace YATECommon.Helpers
{
  /// <summary>
  /// Constant expression evaluator for integer data type
  /// </summary>
  public class IntegerExpressionEvaluator
  {
    #region · Types · 

    /// <summary>
    /// Type of the tokens
    /// </summary>
    protected enum TokenType
    {
      // Do not change the order of enums
      Unknown,

      ParenhesesOpen,
      ParenhesesClose,

      Constant,
      Variable,

      Add,
      Multiply,
      Substract,
      Divide,
      Modulo,
      Or,
      And,
      Not,
      Xor
    }

    /// <summary>
    /// Entries for the operator table
    /// </summary>
    private class TokenTableEntry
    {
      public char TokenChar;
      public TokenType Type;
      public int Precedence;

      public TokenTableEntry(char in_token_char, TokenType in_token_type, int in_precedence)
      {
        TokenChar = in_token_char;
        Type = in_token_type;
        Precedence = in_precedence;
      }
    }

    /// <summary>
    /// List of the operators with precedence
    /// </summary>
    private static readonly TokenTableEntry[] m_token_table =
    {
      new TokenTableEntry('(', TokenType.ParenhesesOpen, 0),
      new TokenTableEntry(')', TokenType.ParenhesesClose, 0),

      new TokenTableEntry('~', TokenType.Not, 1),

      new TokenTableEntry('*', TokenType.Multiply, 2),
      new TokenTableEntry('/', TokenType.Divide, 2),
      new TokenTableEntry('%', TokenType.Modulo, 2),

      new TokenTableEntry('+', TokenType.Add, 3),
      new TokenTableEntry('-', TokenType.Substract, 3),

      new TokenTableEntry('&', TokenType.And, 4),
      new TokenTableEntry('^', TokenType.Xor, 5),
      new TokenTableEntry('|', TokenType.Or, 6)
    };

    /// <summary>
    /// Token class for storing elements of an expression
    /// </summary>
    protected class Token
    {
      public TokenType Type;
      public int Value;
      public string Name;

      /// <summary>
      /// Returns true if token is an operator
      /// </summary>
      public bool IsOperator
      {
        get
        {
          return Type >= TokenType.Add;
        }
      }

      /// <summary>
      /// Convert token to a human readable format
      /// </summary>
      /// <returns></returns>
      public override string ToString()
      {
        switch (Type)
        {
          case TokenType.Constant:
            return Value.ToString();

          case TokenType.Variable:
            return Name + " (" + Value.ToString() + ")";

          default:
            for (int i = 0; i < m_token_table.Length; i++)
            {
              if (Type == m_token_table[i].Type)
                return char.ToString(m_token_table[i].TokenChar);
            }
            return "?";
        }
      }
    }
    #endregion

    #region · Member variables ·
    public Dictionary<string, int> Variables { get; private set; } = new Dictionary<string, int>();
    private List<Token> m_tokens = new List<Token>();
    #endregion

    #region · Public functions ·

    /// <summary>
    /// Parses string to evaluable formula
    /// </summary>
    /// <param name="in_string"></param>
    public void Parse(string in_string)
    {
      // tokenize equation
      Tokenize(in_string);
    }

    /// <summary>
    /// Parses string and evaluate immediatelly
    /// </summary>
    /// <param name="in_string"></param>
    /// <returns></returns>
    public int ParseAndEvaluate(string in_string)
    {
      // tokenize equation
      Tokenize(in_string);

      return Evaluate();
    }

    /// <summary>
    /// Evaluate expression from a string. The expression must contain integer constants (bin,dec,hex) and operators (/*%+-|^~)
    /// </summary>
    /// <returns>Evaluation result</returns>
    public int Evaluate()
    {
      // substitude variables with their values
      for (int i = 0; i < m_tokens.Count; i++)
      {
        if (m_tokens[i].Type == TokenType.Variable)
        {
          if (!Variables.TryGetValue(m_tokens[i].Name, out m_tokens[i].Value))
          {
            throw new ExpresionEvaluatorException("Undefined variable: " + m_tokens[i].Name);
          }
        }
      }

      // find inner opening parentheses
      while (m_tokens.Count > 1)
      {
        int start_index = -1;
        int index = 0;
        while (index < m_tokens.Count)
        {
          if (m_tokens[index].Type == TokenType.ParenhesesOpen)
            start_index = index;

          index++;
        }

        int end_index = -1;
        // find closing parentheses
        index = start_index + 1;
        if (start_index >= 0)
        {
          while (index < m_tokens.Count)
          {
            if (m_tokens[index].Type == TokenType.ParenhesesClose)
            {
              end_index = index;
              break;
            }

            index++;
          }
        }

        if (start_index >= 0 && end_index < 0)
          throw new ExpresionEvaluatorException("Missing parentheses");

        Token start_token = null;
        Token end_token = null;
        if (start_index >= 0)
        {
          start_token = m_tokens[start_index];
          end_token = m_tokens[end_index];
        }
        else
        {
          start_index = 0;
          end_index = m_tokens.Count;
        }

        // evaluate for all precedence level
        for (int precedence = 1; precedence <= m_token_table[m_token_table.Length - 1].Precedence; precedence++)
        {
          Evaluate(start_index, ref end_index, precedence);
        }

        if (start_token != null)
          m_tokens.Remove(start_token);

        if (end_token != null)
          m_tokens.Remove(end_token);

        // if there are more than one token there must be an operator which was processed
        if (start_token == null && end_token == null)
        {
          if (m_tokens.Count > 1)
            throw new ExpresionEvaluatorException("Missing operator");
        }
      }

      if (m_tokens[0].Type != TokenType.Constant)
        throw new ExpresionEvaluatorException("Invalid expression");

      return m_tokens[0].Value;
    }
    #endregion

    #region · Private evaluation functions ·


    /// <summary>
    /// Evaluates an expressnion
    /// </summary>
    /// <param name="in_start_index"></param>
    /// <param name="in_end_index"></param>
    /// <param name="in_precedence"></param>
    private void Evaluate(int in_start_index, ref int in_end_index, int in_precedence)
    {
      // find frist operator with the given precedence
      int first_operator_index = 0;
      while (first_operator_index < m_token_table.Length && m_token_table[first_operator_index].Precedence != in_precedence)
        first_operator_index++;

      for (int token_index = in_start_index; token_index < in_end_index; token_index++)
      {
        // process operators
        if (m_tokens[token_index].IsOperator)
        {
          int operator_index = first_operator_index;
          while (operator_index < m_token_table.Length && m_token_table[operator_index].Precedence == in_precedence && token_index < in_end_index)
          {
            if (m_token_table[operator_index].Type == m_tokens[token_index].Type)
            {
              in_end_index -= EvaluateOperator(token_index);
            }

            operator_index++;
          }
        }
      }
    }

    /// <summary>
    /// Evaluates operator
    /// </summary>
    /// <param name="in_token_index"></param>
    /// <returns></returns>
    private int EvaluateOperator(int in_token_index)
    {
      Token op1 = (in_token_index > 0) ? m_tokens[in_token_index - 1] : null;
      Token op2 = (in_token_index + 1 < m_tokens.Count) ? m_tokens[in_token_index + 1] : null;

      CheckOperator(op2);

      switch (m_tokens[in_token_index].Type)
      {
        case TokenType.Add:
          CheckOperator(op1);
          m_tokens[in_token_index].Value = op1.Value + op2.Value;
          break;

        case TokenType.Multiply:
          CheckOperator(op1);
          m_tokens[in_token_index].Value = op1.Value * op2.Value;
          break;

        case TokenType.Substract:
          CheckOperator(op1);
          m_tokens[in_token_index].Value = op1.Value - op2.Value;
          break;

        case TokenType.Divide:
          CheckOperator(op1);
          if (op2.Value == 0)
            throw new ExpresionEvaluatorException("Division by zero");
          m_tokens[in_token_index].Value = op1.Value / op2.Value;
          break;

        case TokenType.Modulo:
          CheckOperator(op1);
          if (op2.Value == 0)
            throw new ExpresionEvaluatorException("Division by zero");
          m_tokens[in_token_index].Value = op1.Value % op2.Value;
          break;

        case TokenType.Or:
          CheckOperator(op1);
          m_tokens[in_token_index].Value = op1.Value | op2.Value;
          break;

        case TokenType.And:
          CheckOperator(op1);
          m_tokens[in_token_index].Value = op1.Value & op2.Value;
          break;

        case TokenType.Not:
          op1 = null;
          m_tokens[in_token_index].Value = ~op2.Value;
          break;

        case TokenType.Xor:
          CheckOperator(op1);
          m_tokens[in_token_index].Value = op1.Value ^ op2.Value;
          break;
      }

      // set result type
      m_tokens[in_token_index].Type = TokenType.Constant;

      int tokens_removed = 0;

      // remove operands
      if (op1 != null)
      {
        m_tokens.Remove(op1);
        tokens_removed++;
      }

      if (op2 != null)
      {
        m_tokens.Remove(op2);
        tokens_removed++;
      }

      return tokens_removed;
    }

    /// <summary>
    /// Expects operator
    /// </summary>
    /// <param name="in_operator_token"></param>
    private void CheckOperator(Token in_operator_token)
    {
      if (in_operator_token == null || (in_operator_token.Type != TokenType.Constant && in_operator_token.Type != TokenType.Variable))
        throw new ExpresionEvaluatorException("Invalid operator");
    }

    /// <summary>
    /// Tokenizes input string. Converts into series (list) of tokens.
    /// </summary>
    /// <param name="in_string">String to convert</param>
    /// <returns>List of tokens</returns>
    private void Tokenize(string in_string)
    {
      m_tokens.Clear();

      int pos = 0;

      while (pos < in_string.Length)
      {
        Token token;

        // skip space
        if (char.IsWhiteSpace(in_string[pos]))
        {
          pos++;
          continue;
        }

        if (char.IsNumber(in_string[pos]))
        {
          // token is numeric constant
          token = CreateConstantToken(in_string, ref pos);
        }
        else
        {
          // check for unary minus
          if (in_string[pos] == '-' && (m_tokens.Count == 0 || m_tokens[m_tokens.Count - 1].Type != TokenType.Constant))
          {
            // token is numeric constant
            token = CreateConstantToken(in_string, ref pos);
          }
          else
          {
            // token is operator or parentheses or varible
            if (char.IsLetter(in_string[pos]))
            {
              // token is a variable
              token = CreateVariableToken(in_string, ref pos);
              if (!Variables.ContainsKey(token.Name))
                Variables.Add(token.Name, 0);
            }
            else
            {
              // token musts be operator
              token = CreateOperatorToken(in_string, ref pos);
            }
          }
        }

        m_tokens.Add(token);
      }
    }
    #endregion

    #region · Virtual functions · 

    /// <summary>
    /// Creates token from a numeric constant in the string
    /// </summary>
    /// <param name="in_string">String to parse</param>
    /// <param name="inout_pos">Parsing start position</param>
    /// <returns>Parsed constant numerical token</returns>
    protected virtual Token CreateConstantToken(string in_string, ref int inout_pos)
    {
      Token token = new Token();
      bool negative = false;
      int radix = 10;
      int value = 0;
      bool radix_postfix_found = false;
      int pos;

      // get sign
      if (in_string[inout_pos] == '-')
      {
        negative = true;
        inout_pos++;
      }

      // check for radix postfix ('h')
      pos = 0;
      while ((inout_pos + pos) < in_string.Length && IsHexDigit(in_string[inout_pos + pos]))
        pos++;

      if (inout_pos + pos < in_string.Length && char.ToLower(in_string[inout_pos + pos]) == 'h')
      {
        radix = 16;
        radix_postfix_found = true;
      }

      if (!radix_postfix_found)
      {
        // check radix prefix ('0x' or '0b')
        if (in_string[inout_pos] == '0' && in_string.Length > inout_pos + 1)
        {
          switch (char.ToLower(in_string[inout_pos + 1]))
          {
            case 'b':
              radix = 2;
              inout_pos += 2;
              break;

            case 'x':
              radix = 16;
              inout_pos += 2;
              break;
          }
        }
      }

      // convert to number
      bool valid_char = true;
      pos = 0;
      while (valid_char && (inout_pos + pos) < in_string.Length)
      {
        char ch = char.ToLower(in_string[inout_pos + pos]);
        int digit;

        switch (radix)
        {
          case 2:
            digit = ch - '0';

            if (digit < 0 || digit > 1)
              valid_char = false;
            else
              value = (value << 1) + digit;
            break;

          case 10:
            digit = ch - '0';

            if (digit < 0 || digit > 9)
            {
              valid_char = false;
            }
            else
            {
              value = (value * 10) + digit;
            }
            break;

          case 16:
            if (ch >= 'a')
            {
              digit = ch - 'a' + 10;

              if (digit < 10 || digit > 15)
                valid_char = false;
            }
            else
            {
              digit = ch - '0';

              if (digit < 0 || digit > 9)
                valid_char = false;
            }

            if (valid_char)
              value = (value << 4) + digit;
            break;
        }

        if (valid_char)
          pos++;
      }

      if (pos == 0)
        throw new ExpresionEvaluatorException("Invalid number");

      // check terminating character
      if((inout_pos + pos) < in_string.Length)
      {
        // skip postfix radix
        if (char.ToLower(in_string[inout_pos + pos]) == 'h' && radix == 16)
        {
          pos++;
        }
        else
        {
          if (!char.IsWhiteSpace(in_string[inout_pos+pos]) && !IsOperator(in_string[inout_pos+pos]))
          {
            throw new ExpresionEvaluatorException("Invalid number");
          }
        }
      }

      inout_pos += pos;

      token.Type = TokenType.Constant;
      token.Value = (negative) ? -value : value;

      return token;
    }


    /// <summary>
    /// Creates token fromoperator
    /// </summary>
    /// <param name="in_string">String to parse</param>
    /// <param name="inout_pos">Parsing start position</param>
    /// <returns>Operator token</returns>
    protected virtual Token CreateOperatorToken(string in_string, ref int inout_pos)
    {
      Token token = new Token();
      token.Type = TokenType.Unknown;
      char ch = in_string[inout_pos];

      for (int i = 0; i < m_token_table.Length; i++)
      {
        if (m_token_table[i].TokenChar == ch)
        {
          token.Type = m_token_table[i].Type;
          inout_pos++;

          break;
        }
      }

      if (token.Type == TokenType.Unknown)
        throw new ExpresionEvaluatorException("Invalid operator");

      return token;
    }

    /// <summary>
    /// Creates variables token 
    /// </summary>
    /// <param name="in_string">String to parse</param>
    /// <param name="inout_pos">Parsing start position</param>
    /// <returns></returns>
    protected virtual Token CreateVariableToken(string in_string, ref int inout_pos)
    {
      // frist character must be letter
      if (!char.IsLetter(in_string[inout_pos]))
        throw new ExpresionEvaluatorException("Invalid variable");

      Token token = new Token();
      token.Type = TokenType.Variable;

      do
      {
        token.Name += in_string[inout_pos];
        inout_pos++;
      } while (inout_pos < in_string.Length && IsVariableChar(in_string[inout_pos]));

      return token;
    }

    /// <summary>
    /// Check is the given character can be part of a variable name
    /// </summary>
    /// <param name="in_char">Character to check</param>
    /// <returns>True if the character is valid indentifer</returns>
    protected virtual bool IsVariableChar(char in_char)
    {
      return char.IsLetterOrDigit(in_char) || in_char == '_';
    }

    /// <summary>
    /// Checks if the given character is a hexadacimal digit
    /// </summary>
    /// <param name="in_char">True is the character is a hexadecimal digit</param>
    /// <returns></returns>
    protected bool IsHexDigit(char in_char)
    {
      return (in_char >= '0' && in_char <= '9') ||
               (in_char >= 'a' && in_char <= 'f') ||
               (in_char >= 'A' && in_char <= 'F');
    }


    /// <summary>
    /// Checks if the given character is an operator
    /// </summary>
    /// <param name="in_char"></param>
    /// <returns></returns>
    protected bool IsOperator(char in_char)
    {
      for (int i = 0; i < m_token_table.Length; i++)
      {
        if (m_token_table[i].TokenChar == in_char && m_token_table[i].Type != TokenType.ParenhesesOpen && m_token_table[i].Type != TokenType.ParenhesesClose)
        {
          return true;
        }
      }

      return false;
    }
    #endregion
  }
}