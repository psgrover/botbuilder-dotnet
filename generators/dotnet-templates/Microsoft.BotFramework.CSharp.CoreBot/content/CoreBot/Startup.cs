// Generated with CoreBot .NET Template version __vX.X.X__

using CoreBot.Bots;
using CoreBot.Dialogs;
using CoreBot.Dialogs.Sections;
using CoreBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace CoreBot
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
            services.AddControllers().AddNewtonsoftJson();

            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });


            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register LUIS recognizer
            services.AddSingleton<FlightBookingRecognizer>();

            // Register the BookingDialog.
            services.AddSingleton<BookingDialog>();

            // The MainDialog that will be run by the bot.
            services.AddSingleton<MainDialog>();

            // TriageBot Services
            services.AddSingleton<CrmService>(sp => new CrmService(Configuration["ConnectionStrings:PostgreSQL"]));
            services.AddSingleton<ISchedulingService, SchedulingService>();
            services.AddSingleton<OpenAIService>();
            services.AddSingleton<CluRecognizerService>();

            // TriageBot Dialogs
            services.AddSingleton<TriageRootDialog>(sp =>
            {
                var userState = sp.GetRequiredService<UserState>();
                var crmService = sp.GetRequiredService<CrmService>();
                var emailService = sp.GetRequiredService<EmailService>();
                var configuration = sp.GetRequiredService<IConfiguration>();
                var openAIService = sp.GetRequiredService<OpenAIService>();
                var cluService = sp.GetRequiredService<CluRecognizerService>();
                var schedulingService = sp.GetRequiredService<ISchedulingService>();
                return new TriageRootDialog(userState, crmService, emailService, configuration);
            });
            services.AddSingleton<IntroDialog>();
            services.AddSingleton<WhyMeWhyNowDialog>();
            services.AddSingleton<BusinessInfoDialog>();
            services.AddSingleton<WidenGapDialog>();
            services.AddSingleton<WhatsMissingDialog>();
            services.AddSingleton<WhatDoYouNeedDialog>();
            services.AddSingleton<TimingDialog>();
            services.AddSingleton<ProblemCheckInDialog>();
            services.AddSingleton<FitDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            //services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();
            services.AddTransient<IBot>(sp =>
            {
                var conversationState = sp.GetRequiredService<ConversationState>();
                var userState = sp.GetRequiredService<UserState>();
                var dialog = sp.GetRequiredService<TriageRootDialog>();
                var logger = sp.GetRequiredService<ILogger<DialogBot<TriageRootDialog>>>();
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new TriageBot(conversationState, userState, dialog, logger, configuration);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
