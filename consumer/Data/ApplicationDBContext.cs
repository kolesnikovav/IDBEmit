using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace consumer
{
    public class ApplicationDBContext: DbContext
    {
        public DbSet<GoodsCategory> GoodsCategories {get;set;}
        public DbSet<Goods> Goods {get;set;}
        public DbSet<GoodsBarcode> Barcodes {get;set;}
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
        :base(options)
        {}
    }
}