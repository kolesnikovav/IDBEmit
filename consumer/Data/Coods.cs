using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using IDBEmit;

namespace consumer
{
    public enum VAT
    {
        tax10, tax20
    }
    [ClientStorage("Category")]
    public class GoodsCategory
    {
        [Key]
        public int Id {get;set;}
        public string Name {get;set;}
    }
    [ClientStorage]
    public class Goods
    {
        [Key]
        public int Id {get;set;}
        public string Code {get;set;}
        [ClientIndex]
        public string Name {get;set;}
        public GoodsCategory Category {get;set;}
        public decimal Price {get;set;}
        public VAT VAT {get;set;}
    }
    public class GoodsBarcode
    {
        [Key]
        [MaxLength(12)]
        public string Id {get;set;}
        public Goods Goods {get;set;}
    }
}