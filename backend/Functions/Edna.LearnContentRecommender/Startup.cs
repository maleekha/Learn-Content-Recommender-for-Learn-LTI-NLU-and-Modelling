using AutoMapper;
using Edna.LearnContentRecommender;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Edna.Bindings.Assignment;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Edna.LearnContentRecommender
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.Services.AddLogging();

            builder.AddAssignmentBinding();

            builder.Services.AddHttpClient();
            builder.Services.AddAutoMapper(GetType().Assembly);
        }
    }
}
