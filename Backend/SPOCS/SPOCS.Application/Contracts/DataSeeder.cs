using Microsoft.AspNetCore.Identity;
using SPOCS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPOCS.Application.Contracts
{
    public interface IDataSeeder
    {
        Task SeedAsync();
    }
}
