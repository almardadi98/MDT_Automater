using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//Almar
using Microsoft.AspNetCore.Mvc.Formatters;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.Negotiate;

namespace MDTNamer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            //Almar
            services.AddMvc(action =>
            {
                action.ReturnHttpNotAcceptable = true;
                action.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            });
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme);
            services.AddOptions();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>
            (Configuration.GetSection("IpRateLimit"));
            services.AddSingleton<IIpPolicyStore,
            MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore,
            MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration,
            RateLimitConfiguration>();
            services.AddHttpContextAccessor();
            //Almar
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MDTNamer", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MDTNamer v1"));
            }


            //app.UseHttpsRedirection();
            
            //https://www.infoworld.com/article/3442946/how-to-implement-rate-limiting-in-aspnet-core.html
            app.UseIpRateLimiting();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
