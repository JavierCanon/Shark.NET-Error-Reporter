﻿using ErrorReporting.Business.Internal.Contracts;
using ErrorReporting.Dal.Models;
using ErrorReporting.Shared.Tests.Data.Mocked;
using ErrorReporting.Shared.Tests.Exceptions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ErrorReporting.Business.Tests.UnitTests.Managers
{
    [TestFixture]
    public class MockErrorsReportingManagerTest
    {
        IErrorsReportingManager errorsReportingManager;

        public MockErrorsReportingManagerTest() { }

        [Test]
        public void LogError()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingManager> mockManager = new Mock<IErrorsReportingManager>();
            mockManager.Setup(m => m.LogError(It.IsAny<Exception>(), 
                                              It.IsAny<AssemblyName>(), 
                                              It.IsAny<string>()))
                       .Callback<Exception, AssemblyName, string>((exception, assemblyName, errorCode) =>
                       {
                           store.Exceptions.Add(new ErrorReportException
                           {
                               Id = 1,
                               IdApplication = store.Applications.First().Id,
                               Type = exception.GetType().ToString(),
                               Message = exception.Message,
                               Source = exception.Source,
                               SiteModule = (exception.TargetSite != null && exception.TargetSite.Module != null) ? exception.TargetSite.Module.Name : null,
                               SiteName = exception.TargetSite.Name,
                               StackTrace = exception.StackTrace,
                               HelpLink = exception.HelpLink,
                               Date = DateTime.Now,
                               CustomErrorType = errorCode
                           });
                       }).Verifiable();
            this.errorsReportingManager = mockManager.Object;

            try
            {
                ExceptionGenerator.ThrowsOne();
            }
            catch (Exception exception)
            {
                Assert.That(() =>
                {
                    this.errorsReportingManager.LogError(exception, 
                                                         Assembly.GetExecutingAssembly().GetName(), 
                                                         "ErrorType.Specific");
                }, Throws.Nothing);

                mockManager.Verify(m => m.LogError(It.IsAny<Exception>(), It.IsAny<AssemblyName>(), It.IsAny<string>()), Times.Once());
                Assert.AreEqual(1, store.Exceptions.Count);
            }
        }
    }
}
