using blogpessoal.Data;
using blogpessoal.Model;
using blogpessoal.Service.Implements;
using blogpessoal.Service;
using blogpessoal.Validator;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using blogpessoal.Security;
using blogpessoal.Security.Implements;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using blogpessoal.Configuration;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;

namespace blogpessoal {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Add Controller Class
            builder.Services.AddControllers()
                .AddNewtonsoftJson(options => {
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                }
            );

            // Conex�o com o Banco de dados
            if (builder.Configuration["Environment:Start"] == "PROD") {
                // Conex�o com o PostgresSQL - Nuvem
                builder.Configuration.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("secrets.json");

                var connectionString = builder.Configuration.GetConnectionString("ProdConnection");

                builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            } else {
                // Conex�o com o SQL Server - Localhost
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
            }

            // Valida��o das Entidades
            builder.Services.AddTransient<IValidator<Postagem>, PostagemValidator>();
            builder.Services.AddTransient<IValidator<Tema>, TemaValidator>();
            builder.Services.AddTransient<IValidator<User>, UserValidator>();

            // Registrar as Classes e Interfaces Service
            builder.Services.AddScoped<IPostagemService, PostagemService>();
            builder.Services.AddScoped<ITemaService, TemaService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddTransient<IAuthService, AuthService>();

            // Valida��o do Token
            builder.Services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                var key = Encoding.UTF8.GetBytes(Settings.Secret);
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            // Learn more about configuring Swagger/OpenAPI
            // at https://aka.ms/aspnetcore/swashbuckle

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddEndpointsApiExplorer();

            //Registrar o Swagger
            builder.Services.AddSwaggerGen(options => {

                //Personalizar a P�gna inicial do Swagger
                options.SwaggerDoc("v1", new OpenApiInfo {
                    Version = "v1",
                    Title = "Projeto Blog Pessoal",
                    Description = "Projeto Blog Pessoal - ASP.NET Core 7 - Entity Framework",
                    Contact = new OpenApiContact {
                        Name = "Fl�vio Farias",
                        Email = "fs.flaviosilv4@gmail.com",
                        Url = new Uri("https://github.com/oFurabio")
                    },
                    License = new OpenApiLicense {
                        Name = "Github",
                        Url = new Uri("https://github.com/oFurabio")
                    }
                });

                //Adicionar a Seguran�a no Swagger
                options.AddSecurityDefinition("JWT", new OpenApiSecurityScheme {
                    In = ParameterLocation.Header,
                    Description = "Digite um Token JWT v�lido!",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                //Adicionar a configura��o visual da Seguran�a no Swagger
                options.OperationFilter<AuthResponsesOperationFilter>();

            });

            // Adicionar o Fluent Validation no Swagger
            builder.Services.AddFluentValidationRulesToSwagger();

            // Configura��o do CORS
            builder.Services.AddCors(options => {
                options.AddPolicy(name: "MyPolicy",
                    policy => {
                        policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    });
            });

            var app = builder.Build();

            // Criar o Banco de dados e as tabelas Automaticamente
            using (var scope = app.Services.CreateAsyncScope()) {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            }

            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();

            // Swagger Como P�gina Inicial - Nuvem
            if (app.Environment.IsProduction()) {
                app.UseSwaggerUI(options => {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Blog Pessoal - v1");
                    options.RoutePrefix = string.Empty;
                });
            }

            //Habilitar CORS
            app.UseCors("MyPolicy");

            // Habilitar a Autentica��o e a Autoriza��o
            app.UseAuthentication();
            app.UseAuthorization();

            // Habilitar Controller
            app.MapControllers();

            app.Run();
        }
    }
}