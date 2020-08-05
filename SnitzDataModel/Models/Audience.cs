using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using PetaPoco;
using Snitz.Base;
using SnitzDataModel.Database;

namespace SnitzDataModel.Models
{

    #region Audience Repository

    [TableName("Audience", prefixType = Extras.TablePrefixTypes.Forum)]
    [PrimaryKey("ClientId", AutoIncrement = false)]
    [ExplicitColumns]
    public class Audience
    {
        
        [MaxLength(32)]
        [Column("ClientId")]
        public string ClientId { get; set; }

        [MaxLength(80)]
        [Column("Base64Secret")]
        public string Base64Secret { get; set; }

        [MaxLength(100)]
        [Column("Name")]
        public string Name { get; set; }

    }

    public class AudienceModel
    {
        [MaxLength(100)]
        [Required]
        public string Name { get; set; }
    }

    public static class AudiencesStore
    {
        public static ConcurrentDictionary<string, Audience> AudiencesList =
            new ConcurrentDictionary<string, Audience>();

        static AudiencesStore()
        {
            AudiencesList.TryAdd("827573a5c21c41adb91544b4a46afa9d",
                new Audience
                {
                    ClientId = "827573a5c21c41adb91544b4a46afa9d",
                    Base64Secret = "U25pdHpXZWJBcGlfMjAxNw==",
                    Name = "biolog"
                });
        }

        public static Audience AddAudience(string name)
        {
            var clientId = Guid.NewGuid().ToString("N");

            var key = new byte[32];
            RNGCryptoServiceProvider.Create().GetBytes(key);
            var base64Secret = Convert.ToBase64String(key);

            Audience newAudience = new Audience {ClientId = clientId, Base64Secret = base64Secret, Name = name};
            AudiencesList.TryAdd(clientId, newAudience);
            try
            {
                using (var context = new SnitzDataContext())
                {
                    context.Save(newAudience);
                }
            }
            catch (Exception)
            {

                return null;
            }

            return newAudience;
        }

        public static Audience FindAudience(string clientId)
        {
            Audience audience = null;
            if (AudiencesList.TryGetValue(clientId, out audience))
            {
                return audience;
            }
            //cant find the Id so lets try the name
            using (var context = new SnitzDataContext())
            {
                audience = context.SingleOrDefault<Audience>("WHERE Name=@0", clientId);
                if (audience != null)
                {
                    AudiencesList.TryAdd(audience.ClientId, audience);
                    return audience;
                }
            }            
            using (var context = new SnitzDataContext())
            {
                audience = context.GetById<Audience>(clientId);
                if (audience != null)
                {
                    AudiencesList.TryAdd(clientId, audience);
                    return audience;
                }

            }

            return null;
        }

        public static void RemoveAudience(string clientId)
        {
            if (AudiencesList.ContainsKey(clientId))
            {
                Audience audience = null;
                if (AudiencesList.TryRemove(clientId, out audience))
                {
                    using (var context = new SnitzDataContext())
                    {
                        context.Delete<Audience>("WHERE ClientId=@0", clientId);
                    }                    
                }


            }

        }
    }

    #endregion
}