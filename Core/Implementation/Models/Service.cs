﻿using System.ComponentModel;
using Upsilon.Apps.PassKey.Core.Abstraction.Interfaces;

namespace Upsilon.Apps.PassKey.Core.Implementation.Models
{
   internal sealed class Service : IService
   {
      #region IService interface explicit implementation

      string IItem.ItemId => ItemId;
      IUser IService.User => User;
      IAccount[] IService.Accounts => [.. Accounts];

      string IService.ServiceName
      {
         get => ServiceName;
         set => ServiceName = Database.AutoSave.UpdateValue(ItemId,
            itemName: this.ToString(),
            fieldName: nameof(ServiceName),
            needsReview: true,
            value: value,
            readableValue: value);
      }

      string IService.Url
      {
         get => Url;
         set => Url = Database.AutoSave.UpdateValue(ItemId,
            itemName: this.ToString(),
            fieldName: nameof(Url),
            needsReview: false,
            value: value,
            readableValue: value);
      }

      string IService.Notes
      {
         get => Notes;
         set => Notes = Database.AutoSave.UpdateValue(ItemId,
            itemName: this.ToString(),
            fieldName: nameof(Notes),
            needsReview: false,
            value: value,
            readableValue: value);
      }

      IAccount IService.AddAccount(string label, IEnumerable<string> identifiants, string password)
      {
         Account account = new()
         {
            Service = this,
            ItemId = ItemId + Database.CryptographicCenter.GetHash(label + string.Join(string.Empty, identifiants)),
            Label = label,
            Identifiants = identifiants.ToArray(),
            Password = password,
         };
         account.Passwords[DateTime.Now] = password;

         Accounts.Add(Database.AutoSave.AddValue(ItemId, itemName: account.ToString(), containerName: this.ToString(), needsReview: false, account));

         return account;
      }

      void IService.DeleteAccount(IAccount account)
      {
         Account accountToRemove = Accounts.FirstOrDefault(x => x.ItemId == account.ItemId)
            ?? throw new KeyNotFoundException($"The '{account.ItemId}' account was not found into the '{ItemId}' service");

         _ = Accounts.Remove(Database.AutoSave.DeleteValue(ItemId, itemName: accountToRemove.ToString(), containerName: this.ToString(), needsReview: true, accountToRemove));

      }

      #endregion

      internal Database Database => User.Database;

      public string ItemId { get; set; } = string.Empty;

      private User? _user;
      internal User User
      {
         get => _user ?? throw new NullReferenceException(nameof(User));
         set
         {
            _user = value;

            foreach (Account account in Accounts)
            {
               account.Service = this;
            }
         }
      }

      public List<Account> Accounts { get; set; } = [];

      public string ServiceName { get; set; } = string.Empty;
      public string Url { get; set; } = string.Empty;
      public string Notes { get; set; } = string.Empty;

      public void Apply(Change change)
      {
         switch (change.ItemId.Length / Database.CryptographicCenter.HashLength)
         {
            case 2:
               _apply(change);
               break;
            case 3:
               Account account = Accounts.FirstOrDefault(x => change.ItemId.StartsWith(x.ItemId))
                  ?? throw new KeyNotFoundException($"The '{change.ItemId}' account was not found into the '{ItemId}' service");

               account.Apply(change);
               break;
            default:
               throw new InvalidDataException("ItemId not valid");
         }
      }

      private void _apply(Change change)
      {
         switch (change.ActionType)
         {
            case Change.Type.Update:
               switch (change.FieldName)
               {
                  case nameof(ServiceName):
                     ServiceName = Database.SerializationCenter.Deserialize<string>(change.Value);
                     break;
                  case nameof(Url):
                     Url = Database.SerializationCenter.Deserialize<string>(change.Value);
                     break;
                  case nameof(Notes):
                     Notes = Database.SerializationCenter.Deserialize<string>(change.Value);
                     break;
                  default:
                     throw new InvalidDataException("FieldName not valid");
               }
               break;
            case Change.Type.Add:
               Account accountToAdd = Database.SerializationCenter.Deserialize<Account>(change.Value);
               accountToAdd.Service = this;
               Accounts.Add(accountToAdd);
               break;
            case Change.Type.Delete:
               Account accountToDelete = Database.SerializationCenter.Deserialize<Account>(change.Value);
               _ = Accounts.RemoveAll(x => x.ItemId == accountToDelete.ItemId);
               break;
            default:
               throw new InvalidEnumArgumentException(nameof(change.ActionType), (int)change.ActionType, typeof(Change.Type));
         }
      }

      public override string ToString() => $"Service {ServiceName}";
   }
}