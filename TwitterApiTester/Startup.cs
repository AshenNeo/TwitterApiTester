using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TwitterApiTester.Twitter;

//using System.IO;
//using Microsoft.AspNetCore.Identity;
//using TwitterApiTester.Twitter;
using Microsoft.AspNetCore.Authentication.Twitter;
using System.Security.Claims;

namespace TwitterApiTester
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
            /*
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            */
            // ローカル環境のTwitter設定があれば適用する。
            var curDir = Directory.GetCurrentDirectory();
            
            var twitterConfigName = (File.Exists($"{curDir}\\appsettings.twitter.local.json"))
                ? "appsettings.twitter.local.json"
                : "appsettings.twitter.json";
            
            var builder = new ConfigurationBuilder();
            builder
                .SetBasePath(curDir)
                .AddJsonFile(twitterConfigName, optional: true);
            var localConfig = builder.Build();
            services.Configure<TwitterApiToken>(localConfig);


            // Twitterのリクエストトークンを保持するためにセッション変数を使う
            services.AddDistributedMemoryCache();
            services.AddSession();
            services.AddSession(options =>
            {
                options.Cookie.Name = ".TwitterApiTester.Session";
                options.IdleTimeout = TimeSpan.FromSeconds(6000);   // とりあえず100分
                options.Cookie.HttpOnly = true;
            });

            // TweetSharp



            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();           // セッション使用

            app.UseMvc();
        }
    }
}
