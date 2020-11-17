namespace AspNetMigrator.Engine
{
    /// <summary>
    /// Gives a suggestion to the calling code about how to format a message displayed to the user.
    /// </summary>
    public enum UserMessageCategory
    {
        /// <summary>
        /// No special format recommended.
        /// </summary>
        None,

        /// <summary>
        /// Recommending this be subtly emphasized as important and a non-issue.
        /// </summary>
        Info,

        /// <summary>
        /// Recommend this be emphasized as something the user should be concerned about.
        /// </summary>
        Warning
    }

    public class UserMessage
    {
        public string Message { get; set; }

        public UserMessageCategory Category { get; set; }
    }
}
