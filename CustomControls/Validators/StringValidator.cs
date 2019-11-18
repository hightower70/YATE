using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace CustomControls
{
	public class StringValidator : ValidationRule
	{
		#region Data members
		private int m_minimum_length = 0;
		private int m_maximum_length = 0;
		private string m_error_message = "";
		private string m_regex_text = "";
		private bool m_required = false;
		private RegexOptions m_regex_options = RegexOptions.None;
		private bool m_validate_as_url = false;
		#endregion

		#region Properties
		public int MinimumLength
		{
			get { return m_minimum_length; }
			set { m_minimum_length = value; }
		}

		public int MaximumLength
		{
			get { return m_maximum_length; }
			set { m_maximum_length = value; }
		}

		public string ErrorMessage
		{
			get { return m_error_message; }
			set { m_error_message = value; }
		}

		public bool Required
		{
			get { return m_required; }
			set { m_required = value; }
		}

		public bool URLValidation
		{
			 get { return m_validate_as_url; }
			set { m_validate_as_url = value; }
		}

		/// <summary>
		/// Identifies the RegexValidator's RegexText attached property.  
		/// This field is read-only.
		/// </summary>
		public string RegexText
		{
			get { return m_regex_text; }
			set { m_regex_text = value; }
		}

		/// <summary>
		/// Gets/sets the RegexOptions to be used during validation.
		/// This property's default value is 'None'.
		/// </summary>
		public RegexOptions RegexOptions
		{
			get { return m_regex_options; }
			set { m_regex_options = value; }
		}

		#endregion

		#region Member functions
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			ValidationResult result = ValidationResult.ValidResult;

			// get string to validate
			string input_string = (value ?? string.Empty).ToString();

			// Check minimum length and maximum length (if specified)
			if ( (MinimumLength > 0 && input_string.Length < MinimumLength) ||
					 (MaximumLength > 0 && input_string.Length > this.MaximumLength) ||
				   (m_required && input_string.Length == 0))
			{
				// validation failed
				result = new ValidationResult(false, this.ErrorMessage);
			}
			else
			{
				// If there is no regular expression to evaluate,
				// then the data is considered to be valid.
				if (!String.IsNullOrEmpty(this.RegexText))
				{
					// If the string does not match the regex, return a value
					// which indicates failure and provide an error mesasge.
					if (Regex.Match(input_string, this.RegexText, this.RegexOptions).Value != input_string)
						result = new ValidationResult(false, this.ErrorMessage);
				}

				if(m_validate_as_url)
				{
					if (!Uri.IsWellFormedUriString(input_string, UriKind.RelativeOrAbsolute))
						result = new ValidationResult(false, this.ErrorMessage);
				}
			}

			return result;
		}
		#endregion
	}
}
