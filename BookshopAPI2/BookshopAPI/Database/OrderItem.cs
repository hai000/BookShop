﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookshopAPI.Models
{
    [Table("order_item")]
    public class OrderItem
    {
        [Key]
        public long id {  get; set; }
        [Required]
        [ForeignKey("orderId")]
        public long orderId {  get; set; }
        [ForeignKey("productId")]
        public long productId {  get; set; }
        public double price { get; set; }
        public double discount {  get; set; }
        public int quantity {  get; set; }
        public DateTime createdAt {  get; set; }
        public DateTime? updatedAt { get; set; }
    }

}
