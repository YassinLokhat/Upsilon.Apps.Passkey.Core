﻿using Upsilon.Apps.PassKey.Core.Enums;
using Upsilon.Apps.PassKey.Core.Utils;

namespace Upsilon.Apps.PassKey.Core.Models
{
   internal sealed class AutoSave
   {
      private Database? _database;
      internal Database Database
      {
         get => _database ?? throw new NullReferenceException(nameof(Database));
         set => _database = value;
      }

      public Queue<Change> Changes { get; set; } = new();

      internal T UpdateValue<T>(string itemId, string itemName, string fieldName, bool needsReview, T value, string readaableValue) where T : notnull
      {
         _addChange(itemId, itemName, string.Empty, fieldName, Database.SerializationCenter.Serialize(value), readaableValue, needsReview, ChangeType.Update);

         return value;
      }

      internal T AddValue<T>(string itemId, string itemName, string containerName, bool needsReview, T value) where T : notnull
      {
         _addChange(itemId, itemName, containerName, string.Empty, Database.SerializationCenter.Serialize(value), string.Empty, needsReview, ChangeType.Add);

         return value;
      }

      internal T DeleteValue<T>(string itemId, string itemName, string containerName, bool needsReview, T value) where T : notnull
      {
         _addChange(itemId, itemName, containerName, string.Empty, Database.SerializationCenter.Serialize(value), string.Empty, needsReview, ChangeType.Delete);

         return value;
      }

      private void _addChange(string itemId, string itemName, string containerName, string fieldName, string value, string readableValue, bool needsReview, ChangeType action)
      {
         Changes.Enqueue(new Change
         {
            ActionType = action,
            ItemId = itemId,
            FieldName = fieldName,
            Value = value,
         });

         if (Database.AutoSaveFileLocker == null)
         {
            Database.AutoSaveFileLocker = new(Database.CryptographicCenter, Database.SerializationCenter, Database.AutoSaveFile, FileMode.OpenOrCreate);
         }

         Database.AutoSaveFileLocker.Save(this, Database.Passkeys);
         string logMessage;

         switch (action)
         {
            case ChangeType.Add:
               logMessage = $"{itemName} has been added to {containerName}";
               break;
            case ChangeType.Delete:
               logMessage = $"{itemName} has been removed from {containerName}";
               break;
            case ChangeType.Update:
            default:
               logMessage = $"{itemName}'s {fieldName.ToSentenceCase().ToLower()} has been {(string.IsNullOrWhiteSpace(readableValue) ? $"updated" : $"set to {readableValue}")}";
               break;
         }

         Database.Logs.AddLog(logMessage, needsReview);
      }

      internal void MergeChange()
      {
         while (Changes.Count != 0)
         {
            Database.User?.Apply(Changes.Dequeue());
         }

         Clear();
      }

      internal void Clear()
      {
         Changes.Clear();

         Database.AutoSaveFileLocker?.Dispose();
         Database.AutoSaveFileLocker = null;

         if (File.Exists(Database.AutoSaveFile))
         {
            File.Delete(Database.AutoSaveFile);
         }
      }
   }
}