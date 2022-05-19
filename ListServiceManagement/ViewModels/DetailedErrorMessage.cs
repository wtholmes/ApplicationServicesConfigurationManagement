using System;

namespace ListServiceManagement.ViewModels
{
    /// <summary>
    /// Detailed Error Message
    /// </summary>
    public class DetailedErrorMessage
    {
        /// <summary>
        /// Severtiy level for this event.
        /// </summary>
        public String EventLevel { get; set; }

        /// <summary>
        /// The ID for this event.
        /// </summary>
        public Int32 EventID { get; set; }

        /// <summary>
        /// The Event Description
        /// </summary>
        public String Description { get; set; }

        /// <summary>
        /// Details of this event.
        /// </summary>
        public String Details { get; set; }

        /// <summary>
        /// The method in which the even occurred.
        /// </summary>
        public String CalledMethod { get; set; }

        /// <summary>
        /// The Request URL that called this method.
        /// </summary>
        public String RequestURL { get; set; }

        /// <summary>
        /// The Request Body that was included in the call.
        /// </summary>
        public Object RequestBody { get; set; }

        /// <summary>
        /// The intended target Entity for this method call.
        /// </summary>
        public Object Entity { get; set; }
    }
}