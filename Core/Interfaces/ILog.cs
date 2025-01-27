﻿namespace Upsilon.Apps.PassKey.Core.Interfaces
{
   /// <summary>
   /// Represent an event log.
   /// </summary>
   public interface ILog
   {
      /// <summary>
      /// The date and time the event occured.
      /// </summary>
      public DateTime DateTime { get; }

      /// <summary>
      /// The identifiant of the item.
      /// </summary>
      public string ItemName { get; }

      /// <summary>
      /// The event message.
      /// </summary>
      public string Message { get; }

      /// <summary>
      /// Indicate if the current log needs review.
      /// </summary>
      public bool NeedsReview { get; set; }
   }
}
