﻿using ErrorReporting.Dal.Context.Contracts;
using ErrorReporting.Dal.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorReporting.Dal.Context
{
    // https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/powershell
    // Get-Help EntityFramework

    // Enable-Migrations
    // Add-Migration <name>
    // Update-Database

    // -------------------------------------------------

    // Enable-Migrations -MigrationsDirectory "Migrations\ErrorsReporting" -ContextTypeName GenericStructure.Dal.Context.EndObjects.ErrorsReportingContext

    // Add-Migration -ConfigurationTypeName ErrorsReportingConfiguration -Name <something>

    // Update-Database -ConfigurationTypeName ErrorsReportingConfiguration

    public class ErrorsReportingContext : DbContext, IErrorsReportingContext
    {
        public ErrorsReportingContext() : base("name=ErrorsReportingContext") { }


        public virtual IDbSet<ErrorReportApplication> Applications { get; set; }

        public virtual IDbSet<ErrorReportException> Exceptions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var applicationModel = modelBuilder.Entity<ErrorReportApplication>();
            applicationModel.HasKey(t => t.Id);
            applicationModel.Property(t => t.RowVersion).IsFixedLength();

            var exceptionModel = modelBuilder.Entity<ErrorReportException>();
            exceptionModel.HasKey(t => t.Id);
            exceptionModel.Property(t => t.RowVersion).IsFixedLength();
        }
    }
}
