﻿// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

namespace Multitenant.Configuration.Domaining
{
    /// <summary>
    /// Parse Exception
    /// </summary>
    public class ParseException : Exception
    {
        /// <summary>
        /// Reason of exception
        /// </summary>
        public TldRule? WinningRule { get; private set; }

        /// <summary>
        /// Reason of exception
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Parse Exception
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="winningRule"></param>
        public ParseException(string errorMessage, TldRule? winningRule = null)
        {
            ErrorMessage = errorMessage;
            WinningRule = winningRule;
        }

        /// <summary>
        /// Message
        /// </summary>
        public override string Message => ErrorMessage;
    }
}
