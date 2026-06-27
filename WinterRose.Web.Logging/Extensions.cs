using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinterRose.Web.Logging;

public static class Extensions
{
    extension(ILoggingBuilder builder)
    {
        public void UseRecordiumLogger()
        {
            builder.Services.RemoveAll<ILoggerProvider>();
            builder.Services.AddSingleton<ILoggerProvider, RecordiumLoggerProvider>();
        }
    }
}
