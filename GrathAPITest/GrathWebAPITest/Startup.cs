using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using GrathWebAPITest.Authentication;

namespace GrathWebAPITest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            AzureAd = Configuration.GetSection("AzureAd").Get<AzureAdOptions>();
        }

        public IConfiguration Configuration { get; }
        public AzureAdOptions AzureAd { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));

            services.Configure<AzureAdOptions>(Configuration.GetSection("AzureAd"));

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy =>
                {
                    //policy.AddAuthenticationSchemes("Cookie, Bearer");
                    policy.RequireAuthenticatedUser();
                    //policy.RequireRole("Admin");
                    //policy.RequireClaim("editor", "contents");
                });
            });

            services.AddMvc();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });

                // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow)
                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Type = "oauth2",
                    Flow = "implicit",
                    AuthorizationUrl = string.Format(Constants.AuthString, AzureAd.TenantId + Constants.OAuth2Auth),
                    TokenUrl = string.Format(Constants.AuthString, AzureAd.TenantId + "/oauth2/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        //{ "Viewer", "Viewer have the ability to view." },
                        //{ "Admin", "Admins can manage roles." }
                        { "Directory.ReadWrite.All", "Admins can manage roles." }
                        
                    }
                });

                // Assign scope requirements to operations based on AuthorizeAttribute
                c.OperationFilter<SecurityRequirementsOperationFilter>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var stringDict = new Dictionary<string, string>();
            stringDict.Add("resource", AzureAd.ClientId);

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");

                c.ConfigureOAuth2(AzureAd.ClientId, "", "", "GrathWebAPITest", " ", stringDict);
            });

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
