using MessagePack;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace realtime_game.Shared.Models.Entities {
    [Table("users")]
    [MessagePackObject]
    public class User {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public string Login_Id { get; set; }
        [Key(2)]
        public string Password { get; set; }
        [Key(3)]
        public string Display_Name { get; set; }
        [Key(4)]
        public DateTime Created_at { get; set; }
        [Key(5)]
        public DateTime Updated_at { get; set; }
    }
}
