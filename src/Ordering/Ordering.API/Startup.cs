using AutoMapper;
using EventBusRabbitMQ;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Ordering.API.Extentions;
using Ordering.API.RabbitMQ;
using Ordering.Core.Repositories;
using Ordering.Core.Repositories.Base;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Repository;
using Ordering.Infrastructure.Repository.Base;
using RabbitMQ.Client;

namespace Ordering.API
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

            #region SqlServer Dependencies

            // use in-memory database
            services.AddDbContext<OrderContext>(c =>
                c.UseInMemoryDatabase("OrderConnection"));

            //// use real database
            //services.AddDbContext<AspnetRunContext>(c =>
            //    c.UseSqlServer(Configuration.GetConnectionString("OrderConnection")));

            //TODO : burdasin -- connection string ekle

            #endregion

            #region Project Dependencies

            // Add Infrastructure Layer
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IOrderRepository, OrderRepository>();

            // Add MediatR
            services.AddMediatR(this.GetType().Assembly);

            // Add AutoMapper
            services.AddAutoMapper(typeof(Startup));

            #endregion

            #region RabbitMQ Dependencies

            services.AddSingleton<IRabbitMQConnection>(sp =>
            {
                var factory = new ConnectionFactory()
                {
                    HostName = Configuration["EventBus:HostName"]
                };

                if (!string.IsNullOrEmpty(Configuration["EventBus:UserName"]))
                {
                    factory.UserName = Configuration["EventBus:UserName"];
                }

                if (!string.IsNullOrEmpty(Configuration["EventBus:Password"]))
                {
                    factory.Password = Configuration["EventBus:Password"];
                }

                return new RabbitMQConnection(factory);
            });

            services.AddSingleton<EventBusRabbitMQConsumer>();

            #endregion

            #region Swagger Dependencies

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
            });

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //Initilize Rabbit Listener in ApplicationBuilderExtentions
            app.UseRabbitListener();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order API V1");
            });
        }
    }
}
