using MessagePack;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace realtime_game.Shared.Models.Entities {
    [Table("friends")]
    [MessagePackObject]
    public class Friend {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public int User_Id { get; set; }
        [Key(2)]
        public int Sender_User_Id { get; set; }
        [Key(3)]
        public DateTime Created_at { get; set; }
        [Key(4)]
        public DateTime Updated_at { get; set; }
    }
}
