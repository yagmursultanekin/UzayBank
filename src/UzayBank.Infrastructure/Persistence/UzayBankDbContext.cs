using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UzayBank.Domain.Entities;

namespace UzayBank.Infrastructure.Persistence;

public class UzayBankDbContext : DbContext
{
    public UzayBankDbContext(DbContextOptions<UzayBankDbContext> options) : base(options)
    {
    }

    public DbSet <User> Users { get; set; }
    public DbSet <Account> Accounts { get; set; }
    public DbSet <Transaction> Transactions { get; set; }
}
