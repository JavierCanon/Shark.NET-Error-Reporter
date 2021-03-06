﻿using ErrorReporting.Dal.Exceptions;
using ErrorReporting.Dal.Exceptions.CustomTypes;
using ErrorReporting.Dal.Manipulation.Services;
using ErrorReporting.Dal.Manipulation.Services.Contracts;
using ErrorReporting.Dal.Models;
using ErrorReporting.Shared.Tests.Data.Mocked;
using ErrorReporting.Shared.Tests.Exceptions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorReporting.Dal.Tests.UnitTests.Manipulation.Services
{
    [TestFixture]
    public class MockErrorsReportingServiceTest
    {
        public MockErrorsReportingServiceTest() { }

        [Test]
        public void CreateApplication()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingService> mockService = new Mock<IErrorsReportingService>();
            mockService.Setup(s => s.CreateApplication(It.IsAny<string>(), It.IsAny<string>()))
                       .Callback<string, string>((name, version) =>
                       {
                           store.Applications.Add(new ErrorReportApplication
                           {
                               Id = store.Applications.Count + 1,
                               Name = name,
                               Version = version,
                               FirstRunDate = DateTime.Now,
                               RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 },
                               Exceptions = new List<ErrorReportException>()
                           });
                       })
                       .Returns<string, string>((name, version) => store.Applications.Last());

            IErrorsReportingService service = mockService.Object;
            ErrorReportApplication application = service.CreateApplication("TestApplication", "1.0.0.0");

            Assert.AreEqual(5, store.Applications.Count);
            Assert.AreEqual(store.Applications.Last().Id, application.Id);
            Assert.AreEqual(store.Applications.Last().FirstRunDate, application.FirstRunDate);
            Assert.AreEqual(store.Applications.Last().Name, application.Name);
        }


        [Test]
        public void CreateApplication_AlreadyExisting()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingService> mockService = new Mock<IErrorsReportingService>();
            mockService.Setup(s => s.CreateApplication(It.IsIn<string>(store.Applications.Select(a => a.Name)), It.IsAny<string>()))
                       .Throws(new DalException(DalErrorType.SqlUniqueConstraintViolation, "Application already exists"));

            IErrorsReportingService service = mockService.Object;
            
            DalException ex = Assert.Throws<DalException>(() =>
            {
                service.CreateApplication("TestApplicationAlreadyExisting", "1.0.0.0");
            });
            Assert.That(ex.errorType, Is.EqualTo(DalErrorType.SqlUniqueConstraintViolation));
        }

        [Test]
        public void GetApplication()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingService> mockService = new Mock<IErrorsReportingService>();
            mockService.Setup(s => s.GetApplication(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns<string, string>((name, version) => store.Applications.Single(a => a.Name == name && a.Version == version));

            IErrorsReportingService service = mockService.Object;
            ErrorReportApplication application = service.GetApplication("TestApplicationAlreadyExisting", "1.0.0.0");

            Assert.AreEqual(1, application.Id);
            Assert.AreEqual(new DateTime(2000, 1, 1), application.FirstRunDate);
        }

        [Test]
        public void GetApplication_NotExisting()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingService> mockService = new Mock<IErrorsReportingService>();
            mockService.Setup(s => s.GetApplication(It.IsNotIn<string>(store.Applications.Select(a => a.Name)), It.IsAny<string>()))
                       .Returns<string, string>(null);

            IErrorsReportingService service = mockService.Object;

            ErrorReportApplication application = null;
            Assert.That(() =>
            {
                application = service.GetApplication("TestApplicationNotExisting", "1.0.0.0");
            }, Throws.Nothing);

            Assert.IsNull(application);
        }

        [Test]
        public void LogException()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingService> mockService = new Mock<IErrorsReportingService>();
            mockService.Setup(s => s.LogException(It.IsIn<int>(store.Applications.Select(a => a.Id)), 
                                                  It.IsAny<Exception>(),
                                                  It.IsAny<string>()))
                       .Callback<int, Exception, string>((idApplicaton, exception, errorCore) =>
                       {
                           store.Exceptions.Add(new ErrorReportException
                           {
                               Id = 1, 
                               IdApplication = idApplicaton, 
                               Type = exception.GetType().ToString(),
                               Message = exception.Message,
                               Source = exception.Source,
                               SiteModule = (exception.TargetSite != null && exception.TargetSite.Module != null) ? exception.TargetSite.Module.Name : null,
                               SiteName = exception.TargetSite.Name,
                               StackTrace = exception.StackTrace,
                               HelpLink = exception.HelpLink,
                               Date = DateTime.Now, 
                               CustomErrorType = errorCore, 
                           });
                       })
                       .Returns((int?)1);

            IErrorsReportingService service = mockService.Object;

            try
            {
                ExceptionGenerator.ThrowsOne();
            }
            catch (Exception exception)
            {
                int? id = null;
                Assert.That(() =>
                {
                    id = service.LogException(store.Applications.First().Id, exception, "ErrorType.Specific");
                }, Throws.Nothing);

                Assert.IsNotNull(id);

                Assert.AreEqual("One", store.Exceptions.First().Message);
            }
        }

        [Test]
        public void LogException_WithInner()
        {
            VolatileDataset store = new VolatileDataset();

            Mock<IErrorsReportingService> mockService = new Mock<IErrorsReportingService>();
            mockService.Setup(s => s.LogException(It.IsIn<int>(store.Applications.Select(a => a.Id)), 
                                                  It.IsAny<Exception>(),
                                                  It.IsAny<string>()))
                       .Callback<int, Exception, string>((idApplicaton, exception, errorCode) =>
                       {
                           store.Exceptions.Add(new ErrorReportException
                           {
                               Id = 1,
                               IdApplication = idApplicaton,
                               Type = exception.GetType().ToString(),
                               Message = exception.Message,
                               Source = exception.Source,
                               SiteModule = (exception.TargetSite != null && exception.TargetSite.Module != null) ? exception.TargetSite.Module.Name : null,
                               SiteName = exception.TargetSite.Name,
                               StackTrace = exception.StackTrace,
                               HelpLink = exception.HelpLink,
                               Date = DateTime.Now,  
                               CustomErrorType = errorCode,
                               IdInnerException = 2
                           });
                           store.Exceptions.Add(new ErrorReportException
                           {
                               Id = 2,
                               IdApplication = idApplicaton,
                               Type = exception.InnerException.GetType().ToString(),
                               Message = exception.InnerException.Message,
                               Source = exception.InnerException.Source,
                               SiteModule = (exception.InnerException.TargetSite != null && exception.InnerException.TargetSite.Module != null) ? exception.InnerException.TargetSite.Module.Name : null,
                               SiteName = exception.InnerException.TargetSite.Name,
                               StackTrace = exception.InnerException.StackTrace,
                               HelpLink = exception.InnerException.HelpLink,
                               Date = DateTime.Now,
                               CustomErrorType = errorCode
                           });
                       })
                       .Returns((int?)1);

            IErrorsReportingService service = mockService.Object;

            try
            {
                ExceptionGenerator.ThrowsTwo();
            }
            catch (Exception exception)
            {
                int? id = null;
                Assert.That(() =>
                {
                    id = service.LogException(store.Applications.First().Id, exception, "ErrorType.Specific");
                }, Throws.Nothing);

                Assert.IsNotNull(id);

                Assert.AreEqual("One", store.Exceptions.ElementAt(1).Message);
                Assert.AreEqual("Two", store.Exceptions.ElementAt(0).Message);
            }
        }
    }
}
