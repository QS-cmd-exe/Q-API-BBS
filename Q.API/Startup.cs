using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Q.API.Coom;
using Q.API.Respostories.EfContext;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q.API
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
            services.AddControllersWithViews();
            #region swagger  ����swagger����
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Version = "v1",
                    Title = "Q.API",
                    Description = "��ܽӿ��ĵ�",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                    {

                        Name = "QAPI",
                        Email = "442534979@qq.com"
                    }

                });

                var basepath = AppContext.BaseDirectory;
                var xmlpath = Path.Combine(basepath, "Q.API.xml");

                c.IncludeXmlComments(xmlpath, true);

                var xmlmodel = Path.Combine(basepath, "Q.API.Model.xml");
                c.IncludeXmlComments(xmlmodel, true);

            });
            #endregion

            //����ע�� Appsettings����
            services.AddControllers();
            services.AddSingleton(new AppSettings(Configuration));//����ģע��


            #region jwt������֤��Ȩ
            //1��������Ȩ
            services.AddSwaggerGen(c =>
            {
                //����С��
                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

                //��header�����token�����ݵ���̨
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "JWT��Ȩ(���ݽ�������ͷ�н��д���)��ֱ�����¿�������Bearer {token}ע������֮����һ���ո�",
                    Name = "Authorization",//JWTĬ�ϵĲ�������
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,//JWTĬ��Authorization��ŵ�λ��
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey


                });

            });

            //���ж��ɫ����
            services.AddAuthorization(option =>
            {
                option.AddPolicy("client", policy => policy.RequireRole("client").Build());//������ɫ
                option.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                option.AddPolicy("AdminOrSystem", policy => policy.RequireRole("Admin", "System"));//��Ĺ�ϵ
                option.AddPolicy("AdminAndSystem", policy => policy.RequireRole("Admin").RequireRole("System"));//�ҵĹ�ϵ

            });
            //2��������֤
            services.AddAuthentication(x =>
            {
                //����Ĭ��Authorization
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                var audienceConfig = Configuration["Audience:Audience"];
                var sysmmetriceKeyAsBase64 = Configuration["Audience:Secret"];
                var iss= Configuration["Audience:Issuer"];
                var keyByteArray = Encoding.ASCII.GetBytes(sysmmetriceKeyAsBase64);
                var signingKey = new SymmetricSecurityKey(keyByteArray);
                options.TokenValidationParameters = new TokenValidationParameters() {
                ValidateIssuerSigningKey=true,
                IssuerSigningKey=signingKey,
                //��������
                ValidateIssuer=true,
                ValidIssuer=iss,//������
                ValidateAudience=true,
                ValidAudience=audienceConfig,//������
                ValidateLifetime=true,
                ClockSkew=TimeSpan.Zero,//����ǻ������ʱ�䣬Ҳ����˵����ʹ���������˹���ʱ�䣬����ҲҪ���ǽ�ȥ������ʱ��+���壬Ĭ��ʱ�����߷��ӣ�����ֱ������Ϊ0
                RequireExpirationTime=true
                
                };

            });
            #endregion


            #region ����ע��EFcore
            services.AddDbContext<SwiftCodeBbsContext>(o => {
                //ʹ�������� ֻ���ĳ�����м���
                o.UseLazyLoadingProxies().UseSqlServer(@"Data Source=(localdb)\ProjectModels;Initial Catalog=SwiftCodeBbs;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;",oo=>
                oo.MigrationsAssembly("Q.API.Respostories"));//Ǩ�Ƶ������Ŀ��
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
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            #region swagger  �����м��
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                c.RoutePrefix = "";//���۱��ػ��Ƿ���������Ĭ����swaggerҳ��

            });
            #endregion

            app.UseStaticFiles();

            app.UseRouting();

            //������֤
            app.UseAuthentication();
            //��Ȩ�м��
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
