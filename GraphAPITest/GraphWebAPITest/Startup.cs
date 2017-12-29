using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using GraphWebAPITest.Authentication;
using System.IdentityModel.Tokens.Jwt;

namespace GraphWebAPITest
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
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddAzureAdBearer(options => Configuration.Bind("AzureAd", options));

            //services.AddAuthentication(options =>
            //{
            //    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            //})
            //.AddAzureAd(options => Configuration.Bind("AzureAd", options))
            //.AddCookie();
            //.AddOpenIdConnect(options =>
            //{

            //});

            services.Configure<AzureAdOptions>(Configuration.GetSection("AzureAd"));

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy =>
                {
                    policy.AddAuthenticationSchemes("Bearer");
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
                    //Extensions = new Dictionary<string, object> { ""}
                    //Flow = "accessCode",
                    AuthorizationUrl = string.Format(Constants.AuthString, AzureAd.TenantId + Constants.OAuth2Auth),
                    TokenUrl = string.Format(Constants.AuthString, AzureAd.TenantId + Constants.OAuth2Token),
                    Scopes = new Dictionary<string, string>
                    {
                        //OpenIdConnectConstants
                        //{ "openid", "" },
                        { "offline_access", ""},
                        { "https://graph.windows.net/Directory.AccessAsUser.All", "" }
                        //,{ "roles", "roles" }
                        //{ "Directory.ReadWrite.All", "Admins can manage roles." }
                        
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

            app.UseStaticFiles();

            var stringDict = new Dictionary<string, string>();
            stringDict.Add("resource", AzureAd.ClientId);
            stringDict.Add("nonce", "123123");

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
