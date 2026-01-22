using ProductCheckerBack.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ProductCheckerBack.Models.Logging;
using Newtonsoft.Json;
using ProductCheckerBack.ErrorLogging;

namespace ProductCheckerBack
{
    internal class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
    {
        public void Configure(EntityTypeBuilder<ErrorLog> builder)
        {
            builder.Property(c => c.Payload).HasConversion(
                c => JsonConvert.SerializeObject(c, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                c => JsonConvert.DeserializeObject<Payload>(c, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }
    }
}
