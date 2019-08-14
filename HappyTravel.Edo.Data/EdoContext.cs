using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Data.Customers;
using HappyTravel.Edo.Data.Locations;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Data
{
    public class EdoContext : DbContext
    {
        public EdoContext(DbContextOptions<EdoContext> options) : base(options)
        {
        }

        [DbFunction("jsonb_to_string")]
        public static string JsonbToString(string target) 
            => throw new Exception();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasPostgresExtension("postgis")
                .HasPostgresExtension("uuid-ossp");

            builder.Entity<Location>()
                .HasKey(l => l.Id);
            builder.Entity<Location>()
                .Property(l => l.Id)
                .HasDefaultValueSql("uuid_generate_v4()")
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.Coordinates)
                .HasColumnType("geography (point)")
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.Name)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.Locality)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.Country)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.DistanceInMeters)
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.Source)
                .IsRequired();
            builder.Entity<Location>()
                .Property(l => l.Type)
                .IsRequired();

            BuildCountry(builder);
            BuildRegion(builder);
            BuildCustomer(builder);
            BuildCompany(builder);
            BuildCustomerCompanyRelation(builder);
        }
        
         private static void BuildCountry(ModelBuilder builder)
        {
            builder.Entity<Country>()
                .HasKey(c => c.Code);
            builder.Entity<Country>()
                .Property(c => c.Code)
                .IsRequired();
            builder.Entity<Country>()
                .Property(c => c.Names)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Country>()
                .Property(c => c.RegionId)
                .IsRequired();
            builder.Entity<Country>()
                .HasData(
                    new Country
                    {
                        Code = "BI",
                        Names =
                            "{\"ar\":\"بوروندي\",\"en\":\"Burundi\",\"cn\":\"布隆迪\",\"es\":\"Burundi\",\"fr\":\"Burundi\",\"ru\":\"Бурунди\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "BJ",
                        Names =
                            "{\"ar\":\"بنن\",\"en\":\"Benin\",\"cn\":\"贝宁\",\"es\":\"Benin\",\"fr\":\"Bénin\",\"ru\":\"Бенин\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "CI",
                        Names =
                            "{\"ar\":\"كوت ديفوار\",\"en\":\"Côte d’Ivoire\",\"cn\":\"科特迪瓦\",\"es\":\"Côte d’Ivoire\",\"fr\":\"Côte d’Ivoire\",\"ru\":\"Кот-д'Ивуар\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "CV",
                        Names =
                            "{\"ar\":\"كابو فيردي\",\"en\":\"Cabo Verde\",\"cn\":\"佛得角\",\"es\":\"Cabo Verde\",\"fr\":\"Cabo Verde\",\"ru\":\"Кабо-Верде\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "GH",
                        Names =
                            "{\"ar\":\"غانا\",\"en\":\"Ghana\",\"cn\":\"加纳\",\"es\":\"Ghana\",\"fr\":\"Ghana\",\"ru\":\"Гана\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "GN",
                        Names =
                            "{\"ar\":\"غينيا\",\"en\":\"Guinea\",\"cn\":\"几内亚\",\"es\":\"Guinea\",\"fr\":\"Guinée\",\"ru\":\"Гвинея\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "GM",
                        Names =
                            "{\"ar\":\"غامبيا\",\"en\":\"Gambia\",\"cn\":\"冈比亚\",\"es\":\"Gambia\",\"fr\":\"Gambie\",\"ru\":\"Гамбия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "GW",
                        Names =
                            "{\"ar\":\"غينيا - بيساو\",\"en\":\"Guinea-Bissau\",\"cn\":\"几内亚比绍\",\"es\":\"Guinea-Bissau\",\"fr\":\"Guinée-Bissau\",\"ru\":\"Гвинея-Бисау\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "LR",
                        Names =
                            "{\"ar\":\"ليبريا\",\"en\":\"Liberia\",\"cn\":\"利比里亚\",\"es\":\"Liberia\",\"fr\":\"Libéria\",\"ru\":\"Либерия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ML",
                        Names =
                            "{\"ar\":\"مالي\",\"en\":\"Mali\",\"cn\":\"马里\",\"es\":\"Malí\",\"fr\":\"Mali\",\"ru\":\"Мали\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "MR",
                        Names =
                            "{\"ar\":\"موريتانيا\",\"en\":\"Mauritania\",\"cn\":\"毛里塔尼亚\",\"es\":\"Mauritania\",\"fr\":\"Mauritanie\",\"ru\":\"Мавритания\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "NE",
                        Names =
                            "{\"ar\":\"النيجر\",\"en\":\"Niger\",\"cn\":\"尼日尔\",\"es\":\"Níger\",\"fr\":\"Niger\",\"ru\":\"Нигер\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "NG",
                        Names =
                            "{\"ar\":\"نيجيريا\",\"en\":\"Nigeria\",\"cn\":\"尼日利亚\",\"es\":\"Nigeria\",\"fr\":\"Nigéria\",\"ru\":\"Нигерия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SN",
                        Names =
                            "{\"ar\":\"السنغال\",\"en\":\"Senegal\",\"cn\":\"塞内加尔\",\"es\":\"Senegal\",\"fr\":\"Sénégal\",\"ru\":\"Сенегал\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SH",
                        Names =
                            "{\"ar\":\"سانت هيلانة\",\"en\":\"Saint Helena\",\"cn\":\"圣赫勒拿\",\"es\":\"Santa Elena\",\"fr\":\"Sainte-Hélène\",\"ru\":\"Остров Святой Елены\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SL",
                        Names =
                            "{\"ar\":\"سيراليون\",\"en\":\"Sierra Leone\",\"cn\":\"塞拉利昂\",\"es\":\"Sierra Leona\",\"fr\":\"Sierra Leone\",\"ru\":\"Сьерра-Леоне\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "TG",
                        Names =
                            "{\"ar\":\"توغو\",\"en\":\"Togo\",\"cn\":\"多哥\",\"es\":\"Togo\",\"fr\":\"Togo\",\"ru\":\"Того\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "AO",
                        Names =
                            "{\"ar\":\"أنغولا\",\"en\":\"Angola\",\"cn\":\"安哥拉\",\"es\":\"Angola\",\"fr\":\"Angola\",\"ru\":\"Ангола\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "TF",
                        Names =
                            "{\"ar\":\"الأراضي الفرنسية الجنوبية الجنوبية\",\"en\":\"French Southern Territories\",\"cn\":\"法属南方领地\",\"es\":\"Territorio de las Tierras Australes Francesas\",\"fr\":\"Terres australes françaises\",\"ru\":\"Южные земли (французская заморская территория)\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "BF",
                        Names =
                            "{\"ar\":\"بوركينا فاسو\",\"en\":\"Burkina Faso\",\"cn\":\"布基纳法索\",\"es\":\"Burkina Faso\",\"fr\":\"Burkina Faso\",\"ru\":\"Буркина-Фасо\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "KM",
                        Names =
                            "{\"ar\":\"جزر القمر\",\"en\":\"Comoros\",\"cn\":\"科摩罗\",\"es\":\"Comoras\",\"fr\":\"Comores\",\"ru\":\"Коморские Острова\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "DJ",
                        Names =
                            "{\"ar\":\"جيبوتي\",\"en\":\"Djibouti\",\"cn\":\"吉布提\",\"es\":\"Djibouti\",\"fr\":\"Djibouti\",\"ru\":\"Джибути\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ER",
                        Names =
                            "{\"ar\":\"إريتريا\",\"en\":\"Eritrea\",\"cn\":\"厄立特里亚\",\"es\":\"Eritrea\",\"fr\":\"Érythrée\",\"ru\":\"Эритрея\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ET",
                        Names =
                            "{\"ar\":\"إثيوبيا\",\"en\":\"Ethiopia\",\"cn\":\"埃塞俄比亚\",\"es\":\"Etiopía\",\"fr\":\"Éthiopie\",\"ru\":\"Эфиопия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "IO",
                        Names =
                            "{\"ar\":\"المحيط الهندي الإقليم البريطاني في\",\"en\":\"British Indian Ocean Territory\",\"cn\":\"英属印度洋领土\",\"es\":\"Territorio Británico del Océano Índico\",\"fr\":\"Territoire britannique de l'océan Indien\",\"ru\":\"Британская территория в Индийском океане\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "KE",
                        Names =
                            "{\"ar\":\"كينيا\",\"en\":\"Kenya\",\"cn\":\"肯尼亚\",\"es\":\"Kenya\",\"fr\":\"Kenya\",\"ru\":\"Кения\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "MG",
                        Names =
                            "{\"ar\":\"مدغشقر\",\"en\":\"Madagascar\",\"cn\":\"马达加斯加\",\"es\":\"Madagascar\",\"fr\":\"Madagascar\",\"ru\":\"Мадагаскар\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "MZ",
                        Names =
                            "{\"ar\":\"موزامبيق\",\"en\":\"Mozambique\",\"cn\":\"莫桑比克\",\"es\":\"Mozambique\",\"fr\":\"Mozambique\",\"ru\":\"Мозамбик\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "MU",
                        Names =
                            "{\"ar\":\"موريشيوس\",\"en\":\"Mauritius\",\"cn\":\"毛里求斯\",\"es\":\"Mauricio\",\"fr\":\"Maurice\",\"ru\":\"Маврикий\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "MW",
                        Names =
                            "{\"ar\":\"ملاوي\",\"en\":\"Malawi\",\"cn\":\"马拉维\",\"es\":\"Malawi\",\"fr\":\"Malawi\",\"ru\":\"Малави\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "YT",
                        Names =
                            "{\"ar\":\"مايوت\",\"en\":\"Mayotte\",\"cn\":\"马约特\",\"es\":\"Mayotte\",\"fr\":\"Mayotte\",\"ru\":\"Остров Майотта\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "RE",
                        Names =
                            "{\"ar\":\"ريونيون\",\"en\":\"Réunion\",\"cn\":\"留尼汪\",\"es\":\"Reunión\",\"fr\":\"Réunion\",\"ru\":\"Реюньон\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "RW",
                        Names =
                            "{\"ar\":\"رواندا\",\"en\":\"Rwanda\",\"cn\":\"卢旺达\",\"es\":\"Rwanda\",\"fr\":\"Rwanda\",\"ru\":\"Руанда\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SO",
                        Names =
                            "{\"ar\":\"الصومال\",\"en\":\"Somalia\",\"cn\":\"索马里\",\"es\":\"Somalia\",\"fr\":\"Somalie\",\"ru\":\"Сомали\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SS",
                        Names =
                            "{\"ar\":\"جنوب السودان\",\"en\":\"South Sudan\",\"cn\":\"南苏丹\",\"es\":\"Sudán del Sur\",\"fr\":\"Soudan du Sud\",\"ru\":\"Южный Судан\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SC",
                        Names =
                            "{\"ar\":\"سيشيل\",\"en\":\"Seychelles\",\"cn\":\"塞舌尔\",\"es\":\"Seychelles\",\"fr\":\"Seychelles\",\"ru\":\"Сейшельские Острова\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "TZ",
                        Names =
                            "{\"ar\":\"جمهورية تنزانيا المتحدة\",\"en\":\"United Republic of Tanzania\",\"cn\":\"坦桑尼亚联合共和国\",\"es\":\"República Unida de Tanzanía\",\"fr\":\"République-Unie de Tanzanie\",\"ru\":\"Объединенная Республика Танзания\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "UG",
                        Names =
                            "{\"ar\":\"أوغندا\",\"en\":\"Uganda\",\"cn\":\"乌干达\",\"es\":\"Uganda\",\"fr\":\"Ouganda\",\"ru\":\"Уганда\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ZM",
                        Names =
                            "{\"ar\":\"زامبيا\",\"en\":\"Zambia\",\"cn\":\"赞比亚\",\"es\":\"Zambia\",\"fr\":\"Zambie\",\"ru\":\"Замбия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ZW",
                        Names =
                            "{\"ar\":\"زمبابوي\",\"en\":\"Zimbabwe\",\"cn\":\"津巴布韦\",\"es\":\"Zimbabwe\",\"fr\":\"Zimbabwe\",\"ru\":\"Зимбабве\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "DZ",
                        Names =
                            "{\"ar\":\"الجزائر\",\"en\":\"Algeria\",\"cn\":\"阿尔及利亚\",\"es\":\"Argelia\",\"fr\":\"Algérie\",\"ru\":\"Алжир\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "EG",
                        Names =
                            "{\"ar\":\"مصر\",\"en\":\"Egypt\",\"cn\":\"埃及\",\"es\":\"Egipto\",\"fr\":\"Égypte\",\"ru\":\"Египет\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "EH",
                        Names =
                            "{\"ar\":\"الصحراء الغربية\",\"en\":\"Western Sahara\",\"cn\":\"西撒哈拉\",\"es\":\"Sáhara Occidental\",\"fr\":\"Sahara occidental\",\"ru\":\"Западная Сахара\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "LY",
                        Names =
                            "{\"ar\":\"ليبيا\",\"en\":\"Libya\",\"cn\":\"利比亚\",\"es\":\"Libia\",\"fr\":\"Libye\",\"ru\":\"Ливия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "MA",
                        Names =
                            "{\"ar\":\"المغرب\",\"en\":\"Morocco\",\"cn\":\"摩洛哥\",\"es\":\"Marruecos\",\"fr\":\"Maroc\",\"ru\":\"Марокко\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SD",
                        Names =
                            "{\"ar\":\"السودان\",\"en\":\"Sudan\",\"cn\":\"苏丹\",\"es\":\"Sudán\",\"fr\":\"Soudan\",\"ru\":\"Судан\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "TN",
                        Names =
                            "{\"ar\":\"تونس\",\"en\":\"Tunisia\",\"cn\":\"突尼斯\",\"es\":\"Túnez\",\"fr\":\"Tunisie\",\"ru\":\"Тунис\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "CF",
                        Names =
                            "{\"ar\":\"جمهورية أفريقيا الوسطى\",\"en\":\"Central African Republic\",\"cn\":\"中非共和国\",\"es\":\"República Centroafricana\",\"fr\":\"République centrafricaine\",\"ru\":\"Центральноафриканская Республика\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "CM",
                        Names =
                            "{\"ar\":\"الكاميرون\",\"en\":\"Cameroon\",\"cn\":\"喀麦隆\",\"es\":\"Camerún\",\"fr\":\"Cameroun\",\"ru\":\"Камерун\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "CD",
                        Names =
                            "{\"ar\":\"جمهورية الكونغو الديمقراطية\",\"en\":\"Democratic Republic of the Congo\",\"cn\":\"刚果民主共和国\",\"es\":\"República Democrática del Congo\",\"fr\":\"République démocratique du Congo\",\"ru\":\"Демократическая Республика Конго\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "CG",
                        Names =
                            "{\"ar\":\"الكونغو\",\"en\":\"Congo\",\"cn\":\"刚果\",\"es\":\"Congo\",\"fr\":\"Congo\",\"ru\":\"Конго\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "GA",
                        Names =
                            "{\"ar\":\"غابون\",\"en\":\"Gabon\",\"cn\":\"加蓬\",\"es\":\"Gabón\",\"fr\":\"Gabon\",\"ru\":\"Габон\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "GQ",
                        Names =
                            "{\"ar\":\"غينيا الاستوائية\",\"en\":\"Equatorial Guinea\",\"cn\":\"赤道几内亚\",\"es\":\"Guinea Ecuatorial\",\"fr\":\"Guinée équatoriale\",\"ru\":\"Экваториальная Гвинея\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ST",
                        Names =
                            "{\"ar\":\"سان تومي وبرينسيبي\",\"en\":\"Sao Tome and Principe\",\"cn\":\"圣多美和普林西比\",\"es\":\"Santo Tomé y Príncipe\",\"fr\":\"Sao Tomé-et-Principe\",\"ru\":\"Сан-Томе и Принсипи\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "TD",
                        Names =
                            "{\"ar\":\"تشاد\",\"en\":\"Chad\",\"cn\":\"乍得\",\"es\":\"Chad\",\"fr\":\"Tchad\",\"ru\":\"Чад\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "BW",
                        Names =
                            "{\"ar\":\"بوتسوانا\",\"en\":\"Botswana\",\"cn\":\"博茨瓦纳\",\"es\":\"Botswana\",\"fr\":\"Botswana\",\"ru\":\"Ботсвана\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "LS",
                        Names =
                            "{\"ar\":\"ليسوتو\",\"en\":\"Lesotho\",\"cn\":\"莱索托\",\"es\":\"Lesotho\",\"fr\":\"Lesotho\",\"ru\":\"Лесото\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "NA",
                        Names =
                            "{\"ar\":\"ناميبيا\",\"en\":\"Namibia\",\"cn\":\"纳米比亚\",\"es\":\"Namibia\",\"fr\":\"Namibie\",\"ru\":\"Намибия\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "SZ",
                        Names =
                            "{\"ar\":\"إسواتيني\",\"en\":\"Eswatini\",\"cn\":\"斯威士兰\",\"es\":\"Eswatini\",\"fr\":\"Eswatini\",\"ru\":\"Эсватини\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "ZA",
                        Names =
                            "{\"ar\":\"جنوب أفريقيا\",\"en\":\"South Africa\",\"cn\":\"南非\",\"es\":\"Sudáfrica\",\"fr\":\"Afrique du Sud\",\"ru\":\"Южная Африка\"}",
                        RegionId = 2
                    },
                    new Country
                    {
                        Code = "AS",
                        Names =
                            "{\"ar\":\"ساموا الأمريكية\",\"en\":\"American Samoa\",\"cn\":\"美属萨摩亚\",\"es\":\"Samoa Americana\",\"fr\":\"Samoa américaines\",\"ru\":\"Американское Самоа\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "AU",
                        Names =
                            "{\"ar\":\"أستراليا\",\"en\":\"Australia\",\"cn\":\"澳大利亚\",\"es\":\"Australia\",\"fr\":\"Australie\",\"ru\":\"Австралия\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "CC",
                        Names =
                            "{\"ar\":\"جزر كوكس (كيلينغ)\",\"en\":\"Cocos (Keeling) Islands\",\"cn\":\"科科斯（基林）群岛\",\"es\":\"Islas Cocos (Keeling)\",\"fr\":\"Îles des Cocos (Keeling)\",\"ru\":\"Кокосовых (Килинг) островов\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "CX",
                        Names =
                            "{\"ar\":\"جزيرة عيد الميلاد\",\"en\":\"Christmas Island\",\"cn\":\"圣诞岛\",\"es\":\"Isla Christmas\",\"fr\":\"Île Christmas\",\"ru\":\"остров Рождества\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "HM",
                        Names =
                            "{\"ar\":\"جزيرة هيرد وجزر ماكدونالد\",\"en\":\"Heard Island and McDonald Islands\",\"cn\":\"赫德岛和麦克唐纳岛\",\"es\":\"Islas Heard y McDonald\",\"fr\":\"Île Heard-et-Îles MacDonald\",\"ru\":\"Остров Херд и острова Макдональд\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "NF",
                        Names =
                            "{\"ar\":\"جزيرة نورفولك\",\"en\":\"Norfolk Island\",\"cn\":\"诺福克岛\",\"es\":\"Isla Norfolk\",\"fr\":\"Île Norfolk\",\"ru\":\"Остров Норфолк\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "NZ",
                        Names =
                            "{\"ar\":\"نيوزيلندا\",\"en\":\"New Zealand\",\"cn\":\"新西兰\",\"es\":\"Nueva Zelandia\",\"fr\":\"Nouvelle-Zélande\",\"ru\":\"Новая Зеландия\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "FJ",
                        Names =
                            "{\"ar\":\"فيجي\",\"en\":\"Fiji\",\"cn\":\"斐济\",\"es\":\"Fiji\",\"fr\":\"Fidji\",\"ru\":\"Фиджи\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "NC",
                        Names =
                            "{\"ar\":\"كاليدونيا الجديدة\",\"en\":\"New Caledonia\",\"cn\":\"新喀里多尼亚\",\"es\":\"Nueva Caledonia\",\"fr\":\"Nouvelle-Calédonie\",\"ru\":\"Новая Каледония\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "PG",
                        Names =
                            "{\"ar\":\"بابوا غينيا الجديدة\",\"en\":\"Papua New Guinea\",\"cn\":\"巴布亚新几内亚\",\"es\":\"Papua Nueva Guinea\",\"fr\":\"Papouasie-Nouvelle-Guinée\",\"ru\":\"Папуа-Новая Гвинея\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "SB",
                        Names =
                            "{\"ar\":\"جزر سليمان\",\"en\":\"Solomon Islands\",\"cn\":\"所罗门群岛\",\"es\":\"Islas Salomón\",\"fr\":\"Îles Salomon\",\"ru\":\"Соломоновы Острова\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "VU",
                        Names =
                            "{\"ar\":\"فانواتو\",\"en\":\"Vanuatu\",\"cn\":\"瓦努阿图\",\"es\":\"Vanuatu\",\"fr\":\"Vanuatu\",\"ru\":\"Вануату\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "FM",
                        Names =
                            "{\"ar\":\"ميكرونيزيا (ولايات - الموحدة)\",\"en\":\"Micronesia (Federated States of)\",\"cn\":\"密克罗尼西亚联邦\",\"es\":\"Micronesia (Estados Federados de)\",\"fr\":\"Micronésie (États fédérés de)\",\"ru\":\"Микронезия (Федеративные Штаты)\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "GU",
                        Names =
                            "{\"ar\":\"غوام\",\"en\":\"Guam\",\"cn\":\"关岛\",\"es\":\"Guam\",\"fr\":\"Guam\",\"ru\":\"Гуам\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "KI",
                        Names =
                            "{\"ar\":\"كيريباس\",\"en\":\"Kiribati\",\"cn\":\"基里巴斯\",\"es\":\"Kiribati\",\"fr\":\"Kiribati\",\"ru\":\"Кирибати\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "MH",
                        Names =
                            "{\"ar\":\"جزر مارشال\",\"en\":\"Marshall Islands\",\"cn\":\"马绍尔群岛\",\"es\":\"Islas Marshall\",\"fr\":\"Îles Marshall\",\"ru\":\"Маршалловы Острова\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "MP",
                        Names =
                            "{\"ar\":\"جزر ماريانا الشمالية\",\"en\":\"Northern Mariana Islands\",\"cn\":\"北马里亚纳群岛\",\"es\":\"Islas Marianas Septentrionales\",\"fr\":\"Îles Mariannes du Nord\",\"ru\":\"Северные Марианские острова\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "NR",
                        Names =
                            "{\"ar\":\"ناورو\",\"en\":\"Nauru\",\"cn\":\"瑙鲁\",\"es\":\"Nauru\",\"fr\":\"Nauru\",\"ru\":\"Науру\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "PW",
                        Names =
                            "{\"ar\":\"بالاو\",\"en\":\"Palau\",\"cn\":\"帕劳\",\"es\":\"Palau\",\"fr\":\"Palaos\",\"ru\":\"Палау\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "UM",
                        Names =
                            "{\"ar\":\"نائية التابعة للولايات المتحدة\",\"en\":\"United States Minor Outlying Islands\",\"cn\":\"美国本土外小岛屿\",\"es\":\"Islas menores alejadas de Estados Unidos\",\"fr\":\"Îles mineures éloignées des États-Unis\",\"ru\":\"Внешние малые острова Соединенных Штатов\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "CK",
                        Names =
                            "{\"ar\":\"جزر كوك\",\"en\":\"Cook Islands\",\"cn\":\"库克群岛\",\"es\":\"Islas Cook\",\"fr\":\"Îles Cook\",\"ru\":\"Острова Кука\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "NU",
                        Names =
                            "{\"ar\":\"نيوي\",\"en\":\"Niue\",\"cn\":\"纽埃\",\"es\":\"Niue\",\"fr\":\"Nioué\",\"ru\":\"Ниуэ\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "PN",
                        Names =
                            "{\"ar\":\"بيتكرن\",\"en\":\"Pitcairn\",\"cn\":\"皮特凯恩\",\"es\":\"Pitcairn\",\"fr\":\"Pitcairn\",\"ru\":\"Питкэрн\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "PF",
                        Names =
                            "{\"ar\":\"بولينيزيا الفرنسية\",\"en\":\"French Polynesia\",\"cn\":\"法属波利尼西亚\",\"es\":\"Polinesia Francesa\",\"fr\":\"Polynésie française\",\"ru\":\"Французская Полинезия\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "TK",
                        Names =
                            "{\"ar\":\"توكيلاو\",\"en\":\"Tokelau\",\"cn\":\"托克劳\",\"es\":\"Tokelau\",\"fr\":\"Tokélaou\",\"ru\":\"Токелау\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "TO",
                        Names =
                            "{\"ar\":\"تونغا\",\"en\":\"Tonga\",\"cn\":\"汤加\",\"es\":\"Tonga\",\"fr\":\"Tonga\",\"ru\":\"Тонга\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "TV",
                        Names =
                            "{\"ar\":\"توفالو\",\"en\":\"Tuvalu\",\"cn\":\"图瓦卢\",\"es\":\"Tuvalu\",\"fr\":\"Tuvalu\",\"ru\":\"Тувалу\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "WF",
                        Names =
                            "{\"ar\":\"جزر واليس وفوتونا\",\"en\":\"Wallis and Futuna Islands\",\"cn\":\"瓦利斯群岛和富图纳群岛\",\"es\":\"Islas Wallis y Futuna\",\"fr\":\"Îles Wallis-et-Futuna\",\"ru\":\"Острова Уоллис и Футуна\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "WS",
                        Names =
                            "{\"ar\":\"ساموا\",\"en\":\"Samoa\",\"cn\":\"萨摩亚\",\"es\":\"Samoa\",\"fr\":\"Samoa\",\"ru\":\"Самоа\"}",
                        RegionId = 9
                    },
                    new Country
                    {
                        Code = "AG",
                        Names =
                            "{\"ar\":\"أنتيغوا وبربودا\",\"en\":\"Antigua and Barbuda\",\"cn\":\"安提瓜和巴布达\",\"es\":\"Antigua y Barbuda\",\"fr\":\"Antigua-et-Barbuda\",\"ru\":\"Антигуа и Барбуда\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BO",
                        Names =
                            "{\"ar\":\"بوليفيا (دولة - المتعددة القوميات)\",\"en\":\"Bolivia (Plurinational State of)\",\"cn\":\"多民族玻利维亚国\",\"es\":\"Bolivia (Estado Plurinacional de)\",\"fr\":\"Bolivie (État plurinational de)\",\"ru\":\"Боливия (Многонациональное Государство)\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BR",
                        Names =
                            "{\"ar\":\"البرازيل\",\"en\":\"Brazil\",\"cn\":\"巴西\",\"es\":\"Brasil\",\"fr\":\"Brésil\",\"ru\":\"Бразилия\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BV",
                        Names =
                            "{\"ar\":\"جزيرة بوفيت\",\"en\":\"Bouvet Island\",\"cn\":\"布维岛\",\"es\":\"Isla Bouvet\",\"fr\":\"Île Bouvet\",\"ru\":\"Остров Буве\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "CL",
                        Names =
                            "{\"ar\":\"شيلي\",\"en\":\"Chile\",\"cn\":\"智利\",\"es\":\"Chile\",\"fr\":\"Chili\",\"ru\":\"Чили\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "CO",
                        Names =
                            "{\"ar\":\"كولومبيا\",\"en\":\"Colombia\",\"cn\":\"哥伦比亚\",\"es\":\"Colombia\",\"fr\":\"Colombie\",\"ru\":\"Колумбия\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "EC",
                        Names =
                            "{\"ar\":\"إكوادور\",\"en\":\"Ecuador\",\"cn\":\"厄瓜多尔\",\"es\":\"Ecuador\",\"fr\":\"Équateur\",\"ru\":\"Эквадор\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "FK",
                        Names =
                            "{\"ar\":\"جزر فوكلاند (مالفيناس)\",\"en\":\"Falkland Islands (Malvinas)\",\"cn\":\"福克兰群岛（马尔维纳斯）\",\"es\":\"Islas Malvinas (Falkland)\",\"fr\":\"Îles Falkland (Malvinas)\",\"ru\":\"Фолклендские (Мальвинские) острова\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GF",
                        Names =
                            "{\"ar\":\"غيانا الفرنسية\",\"en\":\"French Guiana\",\"cn\":\"法属圭亚那\",\"es\":\"Guayana Francesa\",\"fr\":\"Guyane française\",\"ru\":\"Французская Гвиана\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GY",
                        Names =
                            "{\"ar\":\"غيانا\",\"en\":\"Guyana\",\"cn\":\"圭亚那\",\"es\":\"Guyana\",\"fr\":\"Guyana\",\"ru\":\"Гайана\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "PE",
                        Names =
                            "{\"ar\":\"بيرو\",\"en\":\"Peru\",\"cn\":\"秘鲁\",\"es\":\"Perú\",\"fr\":\"Pérou\",\"ru\":\"Перу\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "PY",
                        Names =
                            "{\"ar\":\"باراغواي\",\"en\":\"Paraguay\",\"cn\":\"巴拉圭\",\"es\":\"Paraguay\",\"fr\":\"Paraguay\",\"ru\":\"Парагвай\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GS",
                        Names =
                            "{\"ar\":\"جورجيا الجنوبية وجزر ساندويتش الجنوبية\",\"en\":\"South Georgia and the South Sandwich Islands\",\"cn\":\"南乔治亚岛和南桑德韦奇岛\",\"es\":\"Georgia del Sur y las Islas Sandwich del Sur\",\"fr\":\"Géorgie du Sud-et-les Îles Sandwich du Sud\",\"ru\":\"Южная Джорджия и Южные Сандвичевы острова\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "SR",
                        Names =
                            "{\"ar\":\"سورينام\",\"en\":\"Suriname\",\"cn\":\"苏里南\",\"es\":\"Suriname\",\"fr\":\"Suriname\",\"ru\":\"Суринам\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "UY",
                        Names =
                            "{\"ar\":\"أوروغواي\",\"en\":\"Uruguay\",\"cn\":\"乌拉圭\",\"es\":\"Uruguay\",\"fr\":\"Uruguay\",\"ru\":\"Уругвай\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "VE",
                        Names =
                            "{\"ar\":\"فنزويلا (جمهورية - البوليفارية)\",\"en\":\"Venezuela (Bolivarian Republic of)\",\"cn\":\"委内瑞拉玻利瓦尔共和国\",\"es\":\"Venezuela (República Bolivariana de)\",\"fr\":\"Venezuela (République bolivarienne du)\",\"ru\":\"Венесуэла (Боливарианская Республика)\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BZ",
                        Names =
                            "{\"ar\":\"بليز\",\"en\":\"Belize\",\"cn\":\"伯利兹\",\"es\":\"Belice\",\"fr\":\"Belize\",\"ru\":\"Белиз\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "CR",
                        Names =
                            "{\"ar\":\"كوستاريكا\",\"en\":\"Costa Rica\",\"cn\":\"哥斯达黎加\",\"es\":\"Costa Rica\",\"fr\":\"Costa Rica\",\"ru\":\"Коста-Рика\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GT",
                        Names =
                            "{\"ar\":\"غواتيمالا\",\"en\":\"Guatemala\",\"cn\":\"危地马拉\",\"es\":\"Guatemala\",\"fr\":\"Guatemala\",\"ru\":\"Гватемала\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "HN",
                        Names =
                            "{\"ar\":\"هندوراس\",\"en\":\"Honduras\",\"cn\":\"洪都拉斯\",\"es\":\"Honduras\",\"fr\":\"Honduras\",\"ru\":\"Гондурас\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "MX",
                        Names =
                            "{\"ar\":\"المكسيك\",\"en\":\"Mexico\",\"cn\":\"墨西哥\",\"es\":\"México\",\"fr\":\"Mexique\",\"ru\":\"Мексика\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "NI",
                        Names =
                            "{\"ar\":\"نيكاراغوا\",\"en\":\"Nicaragua\",\"cn\":\"尼加拉瓜\",\"es\":\"Nicaragua\",\"fr\":\"Nicaragua\",\"ru\":\"Никарагуа\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "PA",
                        Names =
                            "{\"ar\":\"بنما\",\"en\":\"Panama\",\"cn\":\"巴拿马\",\"es\":\"Panamá\",\"fr\":\"Panama\",\"ru\":\"Панама\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "SV",
                        Names =
                            "{\"ar\":\"السلفادور\",\"en\":\"El Salvador\",\"cn\":\"萨尔瓦多\",\"es\":\"El Salvador\",\"fr\":\"El Salvador\",\"ru\":\"Сальвадор\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "AW",
                        Names =
                            "{\"ar\":\"أروبا\",\"en\":\"Aruba\",\"cn\":\"阿鲁巴\",\"es\":\"Aruba\",\"fr\":\"Aruba\",\"ru\":\"Аруба\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "AI",
                        Names =
                            "{\"ar\":\"أنغويلا\",\"en\":\"Anguilla\",\"cn\":\"安圭拉\",\"es\":\"Anguila\",\"fr\":\"Anguilla\",\"ru\":\"Ангилья\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "AR",
                        Names =
                            "{\"ar\":\"الأرجنتين\",\"en\":\"Argentina\",\"cn\":\"阿根廷\",\"es\":\"Argentina\",\"fr\":\"Argentine\",\"ru\":\"Аргентина\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BQ",
                        Names =
                            "{\"ar\":\"بونير وسانت يوستاشيوس وسابا\",\"en\":\"Bonaire, Sint Eustatius and Saba\",\"cn\":\"博纳尔，圣俄斯塔休斯和萨巴\",\"es\":\"Bonaire, San Eustaquio y Saba\",\"fr\":\"Bonaire, Saint-Eustache et Saba\",\"ru\":\"Бонайре, Синт-Эстатиус и Саба\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BM",
                        Names =
                            "{\"ar\":\"برمودا\",\"en\":\"Bermuda\",\"cn\":\"百慕大\",\"es\":\"Bermuda\",\"fr\":\"Bermudes\",\"ru\":\"Бермудские острова\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "CA",
                        Names =
                            "{\"ar\":\"كندا\",\"en\":\"Canada\",\"cn\":\"加拿大\",\"es\":\"Canadá\",\"fr\":\"Canada\",\"ru\":\"Канада\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GL",
                        Names =
                            "{\"ar\":\"غرينلند\",\"en\":\"Greenland\",\"cn\":\"格陵兰\",\"es\":\"Groenlandia\",\"fr\":\"Groenland\",\"ru\":\"Гренландия\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "PM",
                        Names =
                            "{\"ar\":\"سان بيير وميكلون\",\"en\":\"Saint Pierre and Miquelon\",\"cn\":\"圣皮埃尔和密克隆\",\"es\":\"San Pedro y Miquelón\",\"fr\":\"Saint-Pierre-et-Miquelon\",\"ru\":\"Сен-Пьер и Микелон\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "US",
                        Names =
                            "{\"ar\":\"الولايات المتحدة الأمريكية\",\"en\":\"United States of America\",\"cn\":\"美利坚合众国\",\"es\":\"Estados Unidos de América\",\"fr\":\"États-Unis d’Amérique\",\"ru\":\"Соединенные Штаты Америки\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BS",
                        Names =
                            "{\"ar\":\"جزر البهاما\",\"en\":\"Bahamas\",\"cn\":\"巴哈马\",\"es\":\"Bahamas\",\"fr\":\"Bahamas\",\"ru\":\"Багамские Острова\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BL",
                        Names =
                            "{\"ar\":\"سان بارتليمي\",\"en\":\"Saint Barthélemy\",\"cn\":\"圣巴泰勒米\",\"es\":\"San Barthélemy\",\"fr\":\"Saint-Barthélemy\",\"ru\":\"Сен-Бартелеми\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "BB",
                        Names =
                            "{\"ar\":\"بربادوس\",\"en\":\"Barbados\",\"cn\":\"巴巴多斯\",\"es\":\"Barbados\",\"fr\":\"Barbade\",\"ru\":\"Барбадос\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "CU",
                        Names =
                            "{\"ar\":\"كوبا\",\"en\":\"Cuba\",\"cn\":\"古巴\",\"es\":\"Cuba\",\"fr\":\"Cuba\",\"ru\":\"Куба\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "CW",
                        Names =
                            "{\"ar\":\"كوراساو\",\"en\":\"Curaçao\",\"cn\":\"库拉索\",\"es\":\"Curazao\",\"fr\":\"Curaçao\",\"ru\":\"Кюрасао\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "KY",
                        Names =
                            "{\"ar\":\"جزر كايمان\",\"en\":\"Cayman Islands\",\"cn\":\"开曼群岛\",\"es\":\"Islas Caimán\",\"fr\":\"Îles Caïmanes\",\"ru\":\"Кайман острова\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "DM",
                        Names =
                            "{\"ar\":\"دومينيكا\",\"en\":\"Dominica\",\"cn\":\"多米尼克\",\"es\":\"Dominica\",\"fr\":\"Dominique\",\"ru\":\"Доминика\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "DO",
                        Names =
                            "{\"ar\":\"الجمهورية الدومينيكية\",\"en\":\"Dominican Republic\",\"cn\":\"多米尼加\",\"es\":\"República Dominicana\",\"fr\":\"République dominicaine\",\"ru\":\"Доминиканская Республика\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GP",
                        Names =
                            "{\"ar\":\"غوادلوب\",\"en\":\"Guadeloupe\",\"cn\":\"瓜德罗普\",\"es\":\"Guadalupe\",\"fr\":\"Guadeloupe\",\"ru\":\"Гваделупа\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "GD",
                        Names =
                            "{\"ar\":\"غرينادا\",\"en\":\"Grenada\",\"cn\":\"格林纳达\",\"es\":\"Granada\",\"fr\":\"Grenade\",\"ru\":\"Гренада\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "HT",
                        Names =
                            "{\"ar\":\"هايتي\",\"en\":\"Haiti\",\"cn\":\"海地\",\"es\":\"Haití\",\"fr\":\"Haïti\",\"ru\":\"Гаити\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "JM",
                        Names =
                            "{\"ar\":\"جامايكا\",\"en\":\"Jamaica\",\"cn\":\"牙买加\",\"es\":\"Jamaica\",\"fr\":\"Jamaïque\",\"ru\":\"Ямайка\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "KN",
                        Names =
                            "{\"ar\":\"سانت كيتس ونيفس\",\"en\":\"Saint Kitts and Nevis\",\"cn\":\"圣基茨和尼维斯\",\"es\":\"Saint Kitts y Nevis\",\"fr\":\"Saint-Kitts-et-Nevis\",\"ru\":\"Сент-Китс и Невис\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "LC",
                        Names =
                            "{\"ar\":\"سانت لوسيا\",\"en\":\"Saint Lucia\",\"cn\":\"圣卢西亚\",\"es\":\"Santa Lucía\",\"fr\":\"Sainte-Lucie\",\"ru\":\"Сент-Люсия\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "MF",
                        Names =
                            "{\"ar\":\"سانت مارتن (الجزء الفرنسي)\",\"en\":\"Saint Martin (French Part)\",\"cn\":\"圣马丁（法属）\",\"es\":\"San Martín (parte francesa)\",\"fr\":\"Saint-Martin (partie française)\",\"ru\":\"Сен-Мартен (французская часть)\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "MS",
                        Names =
                            "{\"ar\":\"مونتسيرات\",\"en\":\"Montserrat\",\"cn\":\"蒙特塞拉特\",\"es\":\"Montserrat\",\"fr\":\"Montserrat\",\"ru\":\"Монтсеррат\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "MQ",
                        Names =
                            "{\"ar\":\"مارتينيك\",\"en\":\"Martinique\",\"cn\":\"马提尼克\",\"es\":\"Martinica\",\"fr\":\"Martinique\",\"ru\":\"Мартиника\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "PR",
                        Names =
                            "{\"ar\":\"بورتوريكو\",\"en\":\"Puerto Rico\",\"cn\":\"波多黎各\",\"es\":\"Puerto Rico\",\"fr\":\"Porto Rico\",\"ru\":\"Пуэрто-Рико\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "SX",
                        Names =
                            "{\"ar\":\"سانت مارتن (الجزء الهولندي)\",\"en\":\"Sint Maarten (Dutch part)\",\"cn\":\"圣马丁（荷属）\",\"es\":\"San Martín (parte Holandesa)\",\"fr\":\"Saint-Martin (partie néerlandaise)\",\"ru\":\"Синт-Мартен (нидерландская часть)\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "TC",
                        Names =
                            "{\"ar\":\"جزر تركس وكايكوس\",\"en\":\"Turks and Caicos Islands\",\"cn\":\"特克斯和凯科斯群岛\",\"es\":\"Islas Turcas y Caicos\",\"fr\":\"Îles Turques-et-Caïques\",\"ru\":\"Острова Теркс и Кайкос\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "TT",
                        Names =
                            "{\"ar\":\"ترينيداد وتوباغو\",\"en\":\"Trinidad and Tobago\",\"cn\":\"特立尼达和多巴哥\",\"es\":\"Trinidad y Tabago\",\"fr\":\"Trinité-et-Tobago\",\"ru\":\"Тринидад и Тобаго\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "VC",
                        Names =
                            "{\"ar\":\"سانت فنسنت وجزر غرينادين\",\"en\":\"Saint Vincent and the Grenadines\",\"cn\":\"圣文森特和格林纳丁斯\",\"es\":\"San Vicente y las Granadinas\",\"fr\":\"Saint-Vincent-et-les Grenadines\",\"ru\":\"Сент-Винсент и Гренадины\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "VG",
                        Names =
                            "{\"ar\":\"جزر فرجن البريطانية\",\"en\":\"British Virgin Islands\",\"cn\":\"英属维尔京群岛\",\"es\":\"Islas Vírgenes Británicas\",\"fr\":\"Îles Vierges britanniques\",\"ru\":\"Британские Виргинские острова\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "VI",
                        Names =
                            "{\"ar\":\"جزر فرجن التابعة للولايات المتحدة\",\"en\":\"United States Virgin Islands\",\"cn\":\"美属维尔京群岛\",\"es\":\"Islas Vírgenes de los Estados Unidos\",\"fr\":\"Îles Vierges américaines\",\"ru\":\"Виргинские острова Соединенных Штатов\"}",
                        RegionId = 19
                    },
                    new Country
                    {
                        Code = "AM",
                        Names =
                            "{\"ar\":\"أرمينيا\",\"en\":\"Armenia\",\"cn\":\"亚美尼亚\",\"es\":\"Armenia\",\"fr\":\"Arménie\",\"ru\":\"Армения\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "BH",
                        Names =
                            "{\"ar\":\"البحرين\",\"en\":\"Bahrain\",\"cn\":\"巴林\",\"es\":\"Bahrein\",\"fr\":\"Bahreïn\",\"ru\":\"Бахрейн\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "CN",
                        Names =
                            "{\"ar\":\"الصين\",\"en\":\"China\",\"cn\":\"中国\",\"es\":\"China\",\"fr\":\"Chine\",\"ru\":\"Китай\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "HK",
                        Names =
                            "{\"ar\":\"الصين، منطقة هونغ كونغ الإدارية الخاصة\",\"en\":\"China, Hong Kong Special Administrative Region\",\"cn\":\"中国香港特别行政区\",\"es\":\"China, región administrativa especial de Hong Kong\",\"fr\":\"Chine, région administrative spéciale de Hong Kong\",\"ru\":\"Китай, Специальный административный район Гонконг\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "JP",
                        Names =
                            "{\"ar\":\"اليابان\",\"en\":\"Japan\",\"cn\":\"日本\",\"es\":\"Japón\",\"fr\":\"Japon\",\"ru\":\"Япония\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "KR",
                        Names =
                            "{\"ar\":\"جمهورية كوريا\",\"en\":\"Republic of Korea\",\"cn\":\"大韩民国\",\"es\":\"República de Corea\",\"fr\":\"République de Corée\",\"ru\":\"Республика Корея\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "MO",
                        Names =
                            "{\"ar\":\"الصين، منطقة ماكاو الإدارية الخاصة\",\"en\":\"China, Macao Special Administrative Region\",\"cn\":\"中国澳门特别行政区\",\"es\":\"China, región administrativa especial de Macao\",\"fr\":\"Chine, région administrative spéciale de Macao\",\"ru\":\"Китай, Специальный административный район Макао\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "MN",
                        Names =
                            "{\"ar\":\"منغوليا\",\"en\":\"Mongolia\",\"cn\":\"蒙古\",\"es\":\"Mongolia\",\"fr\":\"Mongolie\",\"ru\":\"Монголия\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "KP",
                        Names =
                            "{\"ar\":\"جمهورية كوريا الشعبية الديمقراطية\",\"en\":\"Democratic People's Republic of Korea\",\"cn\":\"朝鲜民主主义人民共和国\",\"es\":\"República Popular Democrática de Corea\",\"fr\":\"République populaire démocratique de Corée\",\"ru\":\"Корейская Народно-Демократическая Республика\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "BT",
                        Names =
                            "{\"ar\":\"بوتان\",\"en\":\"Bhutan\",\"cn\":\"不丹\",\"es\":\"Bhután\",\"fr\":\"Bhoutan\",\"ru\":\"Бутан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "IN",
                        Names =
                            "{\"ar\":\"الهند\",\"en\":\"India\",\"cn\":\"印度\",\"es\":\"India\",\"fr\":\"Inde\",\"ru\":\"Индия\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "IR",
                        Names =
                            "{\"ar\":\"إيران (جمهورية - الإسلامية)\",\"en\":\"Iran (Islamic Republic of)\",\"cn\":\"伊朗伊斯兰共和国\",\"es\":\"Irán (República Islámica del)\",\"fr\":\"Iran (République islamique d’)\",\"ru\":\"Иран (Исламская Республика)\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "LK",
                        Names =
                            "{\"ar\":\"سري لانكا\",\"en\":\"Sri Lanka\",\"cn\":\"斯里兰卡\",\"es\":\"Sri Lanka\",\"fr\":\"Sri Lanka\",\"ru\":\"Шри-Ланка\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "MV",
                        Names =
                            "{\"ar\":\"ملديف\",\"en\":\"Maldives\",\"cn\":\"马尔代夫\",\"es\":\"Maldivas\",\"fr\":\"Maldives\",\"ru\":\"Мальдивские Острова\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "NP",
                        Names =
                            "{\"ar\":\"نيبال\",\"en\":\"Nepal\",\"cn\":\"尼泊尔\",\"es\":\"Nepal\",\"fr\":\"Népal\",\"ru\":\"Непал\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "PK",
                        Names =
                            "{\"ar\":\"باكستان\",\"en\":\"Pakistan\",\"cn\":\"巴基斯坦\",\"es\":\"Pakistán\",\"fr\":\"Pakistan\",\"ru\":\"Пакистан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "BN",
                        Names =
                            "{\"ar\":\"بروني دار السلام\",\"en\":\"Brunei Darussalam\",\"cn\":\"文莱达鲁萨兰国\",\"es\":\"Brunei Darussalam\",\"fr\":\"Brunéi Darussalam\",\"ru\":\"Бруней-Даруссалам\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "ID",
                        Names =
                            "{\"ar\":\"إندونيسيا\",\"en\":\"Indonesia\",\"cn\":\"印度尼西亚\",\"es\":\"Indonesia\",\"fr\":\"Indonésie\",\"ru\":\"Индонезия\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "KH",
                        Names =
                            "{\"ar\":\"كمبوديا\",\"en\":\"Cambodia\",\"cn\":\"柬埔寨\",\"es\":\"Camboya\",\"fr\":\"Cambodge\",\"ru\":\"Камбоджа\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "LA",
                        Names =
                            "{\"ar\":\"جمهورية لاو الديمقراطية الشعبية\",\"en\":\"Lao People's Democratic Republic\",\"cn\":\"老挝人民民主共和国\",\"es\":\"República Democrática Popular Lao\",\"fr\":\"République démocratique populaire lao\",\"ru\":\"Лаосская Народно-Демократическая Республика\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "MM",
                        Names =
                            "{\"ar\":\"ميانمار\",\"en\":\"Myanmar\",\"cn\":\"缅甸\",\"es\":\"Myanmar\",\"fr\":\"Myanmar\",\"ru\":\"Мьянма\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "MY",
                        Names =
                            "{\"ar\":\"ماليزيا\",\"en\":\"Malaysia\",\"cn\":\"马来西亚\",\"es\":\"Malasia\",\"fr\":\"Malaisie\",\"ru\":\"Малайзия\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "PH",
                        Names =
                            "{\"ar\":\"الفلبين\",\"en\":\"Philippines\",\"cn\":\"菲律宾\",\"es\":\"Filipinas\",\"fr\":\"Philippines\",\"ru\":\"Филиппины\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "SG",
                        Names =
                            "{\"ar\":\"سنغافورة\",\"en\":\"Singapore\",\"cn\":\"新加坡\",\"es\":\"Singapur\",\"fr\":\"Singapour\",\"ru\":\"Сингапур\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "TH",
                        Names =
                            "{\"ar\":\"تايلند\",\"en\":\"Thailand\",\"cn\":\"泰国\",\"es\":\"Tailandia\",\"fr\":\"Thaïlande\",\"ru\":\"Таиланд\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "TL",
                        Names =
                            "{\"ar\":\"تيمور - ليشتي\",\"en\":\"Timor-Leste\",\"cn\":\"东帝汶\",\"es\":\"Timor-Leste\",\"fr\":\"Timor-Leste\",\"ru\":\"Тимор-Лешти\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "VN",
                        Names =
                            "{\"ar\":\"فييت نام\",\"en\":\"Viet Nam\",\"cn\":\"越南\",\"es\":\"Viet Nam\",\"fr\":\"Viet Nam\",\"ru\":\"Вьетнам\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "AF",
                        Names =
                            "{\"ar\":\"أفغانستان\",\"en\":\"Afghanistan\",\"cn\":\"阿富汗\",\"es\":\"Afganistán\",\"fr\":\"Afghanistan\",\"ru\":\"Афганистан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "AE",
                        Names =
                            "{\"ar\":\"الإمارات العربية المتحدة\",\"en\":\"United Arab Emirates\",\"cn\":\"阿拉伯联合酋长国\",\"es\":\"Emiratos Árabes Unidos\",\"fr\":\"Émirats arabes unis\",\"ru\":\"Объединенные Арабские Эмираты\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "AZ",
                        Names =
                            "{\"ar\":\"أذربيجان\",\"en\":\"Azerbaijan\",\"cn\":\"阿塞拜疆\",\"es\":\"Azerbaiyán\",\"fr\":\"Azerbaïdjan\",\"ru\":\"Азербайджан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "BD",
                        Names =
                            "{\"ar\":\"بنغلاديش\",\"en\":\"Bangladesh\",\"cn\":\"孟加拉国\",\"es\":\"Bangladesh\",\"fr\":\"Bangladesh\",\"ru\":\"Бангладеш\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "KZ",
                        Names =
                            "{\"ar\":\"كازاخستان\",\"en\":\"Kazakhstan\",\"cn\":\"哈萨克斯坦\",\"es\":\"Kazajstán\",\"fr\":\"Kazakhstan\",\"ru\":\"Казахстан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "KG",
                        Names =
                            "{\"ar\":\"قيرغيزستان\",\"en\":\"Kyrgyzstan\",\"cn\":\"吉尔吉斯斯坦\",\"es\":\"Kirguistán\",\"fr\":\"Kirghizistan\",\"ru\":\"Кыргызстан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "TJ",
                        Names =
                            "{\"ar\":\"طاجيكستان\",\"en\":\"Tajikistan\",\"cn\":\"塔吉克斯坦\",\"es\":\"Tayikistán\",\"fr\":\"Tadjikistan\",\"ru\":\"Таджикистан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "TM",
                        Names =
                            "{\"ar\":\"تركمانستان\",\"en\":\"Turkmenistan\",\"cn\":\"土库曼斯坦\",\"es\":\"Turkmenistán\",\"fr\":\"Turkménistan\",\"ru\":\"Туркменистан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "UZ",
                        Names =
                            "{\"ar\":\"أوزبكستان\",\"en\":\"Uzbekistan\",\"cn\":\"乌兹别克斯坦\",\"es\":\"Uzbekistán\",\"fr\":\"Ouzbékistan\",\"ru\":\"Узбекистан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "CY",
                        Names =
                            "{\"ar\":\"قبرص\",\"en\":\"Cyprus\",\"cn\":\"塞浦路斯\",\"es\":\"Chipre\",\"fr\":\"Chypre\",\"ru\":\"Кипр\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "GE",
                        Names =
                            "{\"ar\":\"جورجيا\",\"en\":\"Georgia\",\"cn\":\"格鲁吉亚\",\"es\":\"Georgia\",\"fr\":\"Géorgie\",\"ru\":\"Грузия\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "IQ",
                        Names =
                            "{\"ar\":\"العراق\",\"en\":\"Iraq\",\"cn\":\"伊拉克\",\"es\":\"Iraq\",\"fr\":\"Iraq\",\"ru\":\"Ирак\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "IL",
                        Names =
                            "{\"ar\":\"إسرائيل\",\"en\":\"Israel\",\"cn\":\"以色列\",\"es\":\"Israel\",\"fr\":\"Israël\",\"ru\":\"Израиль\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "JO",
                        Names =
                            "{\"ar\":\"الأردن\",\"en\":\"Jordan\",\"cn\":\"约旦\",\"es\":\"Jordania\",\"fr\":\"Jordanie\",\"ru\":\"Иордания\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "KW",
                        Names =
                            "{\"ar\":\"الكويت\",\"en\":\"Kuwait\",\"cn\":\"科威特\",\"es\":\"Kuwait\",\"fr\":\"Koweït\",\"ru\":\"Кувейт\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "LB",
                        Names =
                            "{\"ar\":\"لبنان\",\"en\":\"Lebanon\",\"cn\":\"黎巴嫩\",\"es\":\"Líbano\",\"fr\":\"Liban\",\"ru\":\"Ливан\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "OM",
                        Names =
                            "{\"ar\":\"عمان\",\"en\":\"Oman\",\"cn\":\"阿曼\",\"es\":\"Omán\",\"fr\":\"Oman\",\"ru\":\"Оман\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "PS",
                        Names =
                            "{\"ar\":\"دولة فلسطين\",\"en\":\"State of Palestine\",\"cn\":\"巴勒斯坦国\",\"es\":\"Estado de Palestina\",\"fr\":\"État de Palestine\",\"ru\":\"Государство Палестина\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "QA",
                        Names =
                            "{\"ar\":\"قطر\",\"en\":\"Qatar\",\"cn\":\"卡塔尔\",\"es\":\"Qatar\",\"fr\":\"Qatar\",\"ru\":\"Катар\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "SA",
                        Names =
                            "{\"ar\":\"المملكة العربية السعودية\",\"en\":\"Saudi Arabia\",\"cn\":\"沙特阿拉伯\",\"es\":\"Arabia Saudita\",\"fr\":\"Arabie saoudite\",\"ru\":\"Саудовская Аравия\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "SY",
                        Names =
                            "{\"ar\":\"الجمهورية العربية السورية\",\"en\":\"Syrian Arab Republic\",\"cn\":\"阿拉伯叙利亚共和国\",\"es\":\"República Árabe Siria\",\"fr\":\"République arabe syrienne\",\"ru\":\"Сирийская Арабская Республика\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "TR",
                        Names =
                            "{\"ar\":\"تركيا\",\"en\":\"Turkey\",\"cn\":\"土耳其\",\"es\":\"Turquía\",\"fr\":\"Turquie\",\"ru\":\"Турция\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "YE",
                        Names =
                            "{\"ar\":\"اليمن\",\"en\":\"Yemen\",\"cn\":\"也门\",\"es\":\"Yemen\",\"fr\":\"Yémen\",\"ru\":\"Йемен\"}",
                        RegionId = 142
                    },
                    new Country
                    {
                        Code = "AX",
                        Names =
                            "{\"ar\":\"جزر ألاند\",\"en\":\"Åland Islands\",\"cn\":\"奥兰群岛\",\"es\":\"Islas Åland\",\"fr\":\"Îles d’Åland\",\"ru\":\"Аландских островов\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "BA",
                        Names =
                            "{\"ar\":\"البوسنة والهرسك\",\"en\":\"Bosnia and Herzegovina\",\"cn\":\"波斯尼亚和黑塞哥维那\",\"es\":\"Bosnia y Herzegovina\",\"fr\":\"Bosnie-Herzégovine\",\"ru\":\"Босния и Герцеговина\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "ES",
                        Names =
                            "{\"ar\":\"إسبانيا\",\"en\":\"Spain\",\"cn\":\"西班牙\",\"es\":\"España\",\"fr\":\"Espagne\",\"ru\":\"Испания\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "GI",
                        Names =
                            "{\"ar\":\"جبل طارق\",\"en\":\"Gibraltar\",\"cn\":\"直布罗陀\",\"es\":\"Gibraltar\",\"fr\":\"Gibraltar\",\"ru\":\"Гибралтар\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "GR",
                        Names =
                            "{\"ar\":\"اليونان\",\"en\":\"Greece\",\"cn\":\"希腊\",\"es\":\"Grecia\",\"fr\":\"Grèce\",\"ru\":\"Греция\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "HR",
                        Names =
                            "{\"ar\":\"كرواتيا\",\"en\":\"Croatia\",\"cn\":\"克罗地亚\",\"es\":\"Croacia\",\"fr\":\"Croatie\",\"ru\":\"Хорватия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "IT",
                        Names =
                            "{\"ar\":\"إيطاليا\",\"en\":\"Italy\",\"cn\":\"意大利\",\"es\":\"Italia\",\"fr\":\"Italie\",\"ru\":\"Италия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "MK",
                        Names =
                            "{\"ar\":\"مقدونيا الشمالية\",\"en\":\"North Macedonia\",\"cn\":\"北马其顿\",\"es\":\"Macedonia del Norte\",\"fr\":\"Macédoine du Nord\",\"ru\":\"Северная Македония\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "MT",
                        Names =
                            "{\"ar\":\"مالطة\",\"en\":\"Malta\",\"cn\":\"马耳他\",\"es\":\"Malta\",\"fr\":\"Malte\",\"ru\":\"Мальта\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "ME",
                        Names =
                            "{\"ar\":\"الجبل الأسود\",\"en\":\"Montenegro\",\"cn\":\"黑山\",\"es\":\"Montenegro\",\"fr\":\"Monténégro\",\"ru\":\"Черногория\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "PT",
                        Names =
                            "{\"ar\":\"البرتغال\",\"en\":\"Portugal\",\"cn\":\"葡萄牙\",\"es\":\"Portugal\",\"fr\":\"Portugal\",\"ru\":\"Португалия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "SM",
                        Names =
                            "{\"ar\":\"سان مارينو\",\"en\":\"San Marino\",\"cn\":\"圣马力诺\",\"es\":\"San Marino\",\"fr\":\"Saint-Marin\",\"ru\":\"Сан-Марино\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "RS",
                        Names =
                            "{\"ar\":\"صربيا\",\"en\":\"Serbia\",\"cn\":\"塞尔维亚\",\"es\":\"Serbia\",\"fr\":\"Serbie\",\"ru\":\"Сербия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "SI",
                        Names =
                            "{\"ar\":\"سلوفينيا\",\"en\":\"Slovenia\",\"cn\":\"斯洛文尼亚\",\"es\":\"Eslovenia\",\"fr\":\"Slovénie\",\"ru\":\"Словения\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "VA",
                        Names =
                            "{\"ar\":\"الكرسي الرسولي\",\"en\":\"Holy See\",\"cn\":\"教廷\",\"es\":\"Santa Sede\",\"fr\":\"Saint-Siège\",\"ru\":\"Святой Престол\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "AL",
                        Names =
                            "{\"ar\":\"ألبانيا\",\"en\":\"Albania\",\"cn\":\"阿尔巴尼亚\",\"es\":\"Albania\",\"fr\":\"Albanie\",\"ru\":\"Албания\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "AD",
                        Names =
                            "{\"ar\":\"أندورا\",\"en\":\"Andorra\",\"cn\":\"安道尔\",\"es\":\"Andorra\",\"fr\":\"Andorre\",\"ru\":\"Андорра\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "AT",
                        Names =
                            "{\"ar\":\"النمسا\",\"en\":\"Austria\",\"cn\":\"奥地利\",\"es\":\"Austria\",\"fr\":\"Autriche\",\"ru\":\"Австрия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "BE",
                        Names =
                            "{\"ar\":\"بلجيكا\",\"en\":\"Belgium\",\"cn\":\"比利时\",\"es\":\"Bélgica\",\"fr\":\"Belgique\",\"ru\":\"Бельгия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "BG",
                        Names =
                            "{\"ar\":\"بلغاريا\",\"en\":\"Bulgaria\",\"cn\":\"保加利亚\",\"es\":\"Bulgaria\",\"fr\":\"Bulgarie\",\"ru\":\"Болгария\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "BY",
                        Names =
                            "{\"ar\":\"بيلاروس\",\"en\":\"Belarus\",\"cn\":\"白俄罗斯\",\"es\":\"Belarús\",\"fr\":\"Bélarus\",\"ru\":\"Беларусь\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "CZ",
                        Names =
                            "{\"ar\":\"تشيكيا\",\"en\":\"Czechia\",\"cn\":\"捷克\",\"es\":\"Chequia\",\"fr\":\"Tchéquie\",\"ru\":\"Чехия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "HU",
                        Names =
                            "{\"ar\":\"هنغاريا\",\"en\":\"Hungary\",\"cn\":\"匈牙利\",\"es\":\"Hungría\",\"fr\":\"Hongrie\",\"ru\":\"Венгрия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "MD",
                        Names =
                            "{\"ar\":\"جمهورية مولدوفا\",\"en\":\"Republic of Moldova\",\"cn\":\"摩尔多瓦共和国\",\"es\":\"República de Moldova\",\"fr\":\"République de Moldova\",\"ru\":\"Республика Молдова\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "PL",
                        Names =
                            "{\"ar\":\"بولندا\",\"en\":\"Poland\",\"cn\":\"波兰\",\"es\":\"Polonia\",\"fr\":\"Pologne\",\"ru\":\"Польша\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "RO",
                        Names =
                            "{\"ar\":\"رومانيا\",\"en\":\"Romania\",\"cn\":\"罗马尼亚\",\"es\":\"Rumania\",\"fr\":\"Roumanie\",\"ru\":\"Румыния\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "RU",
                        Names =
                            "{\"ar\":\"الاتحاد الروسي\",\"en\":\"Russian Federation\",\"cn\":\"俄罗斯联邦\",\"es\":\"Federación de Rusia\",\"fr\":\"Fédération de Russie\",\"ru\":\"Российская Федерация\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "SK",
                        Names =
                            "{\"ar\":\"سلوفاكيا\",\"en\":\"Slovakia\",\"cn\":\"斯洛伐克\",\"es\":\"Eslovaquia\",\"fr\":\"Slovaquie\",\"ru\":\"Словакия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "UA",
                        Names =
                            "{\"ar\":\"أوكرانيا\",\"en\":\"Ukraine\",\"cn\":\"乌克兰\",\"es\":\"Ucrania\",\"fr\":\"Ukraine\",\"ru\":\"Украина\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "DK",
                        Names =
                            "{\"ar\":\"الدانمرك\",\"en\":\"Denmark\",\"cn\":\"丹麦\",\"es\":\"Dinamarca\",\"fr\":\"Danemark\",\"ru\":\"Дания\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "EE",
                        Names =
                            "{\"ar\":\"إستونيا\",\"en\":\"Estonia\",\"cn\":\"爱沙尼亚\",\"es\":\"Estonia\",\"fr\":\"Estonie\",\"ru\":\"Эстония\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "FI",
                        Names =
                            "{\"ar\":\"فنلندا\",\"en\":\"Finland\",\"cn\":\"芬兰\",\"es\":\"Finlandia\",\"fr\":\"Finlande\",\"ru\":\"Финляндия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "FO",
                        Names =
                            "{\"ar\":\"جزر فايرو\",\"en\":\"Faroe Islands\",\"cn\":\"法罗群岛\",\"es\":\"Islas Feroe\",\"fr\":\"Îles Féroé\",\"ru\":\"Фарерские острова\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "GB",
                        Names =
                            "{\"ar\":\"المملكة المتحدة لبريطانيا العظمى وآيرلندا الشمالية\",\"en\":\"United Kingdom of Great Britain and Northern Ireland\",\"cn\":\"大不列颠及北爱尔兰联合王国\",\"es\":\"Reino Unido de Gran Bretaña e Irlanda del Norte\",\"fr\":\"Royaume-Uni de Grande-Bretagne et d’Irlande du Nord\",\"ru\":\"Соединенное Королевство Великобритании и Северной Ирландии\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "IM",
                        Names =
                            "{\"ar\":\"جزيرة مان\",\"en\":\"Isle of Man\",\"cn\":\"马恩岛\",\"es\":\"Isla de Man\",\"fr\":\"Île de Man\",\"ru\":\"Остров Мэн\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "IE",
                        Names =
                            "{\"ar\":\"آيرلندا\",\"en\":\"Ireland\",\"cn\":\"爱尔兰\",\"es\":\"Irlanda\",\"fr\":\"Irlande\",\"ru\":\"Ирландия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "IS",
                        Names =
                            "{\"ar\":\"آيسلندا\",\"en\":\"Iceland\",\"cn\":\"冰岛\",\"es\":\"Islandia\",\"fr\":\"Islande\",\"ru\":\"Исландия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "LT",
                        Names =
                            "{\"ar\":\"ليتوانيا\",\"en\":\"Lithuania\",\"cn\":\"立陶宛\",\"es\":\"Lituania\",\"fr\":\"Lituanie\",\"ru\":\"Литва\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "LV",
                        Names =
                            "{\"ar\":\"لاتفيا\",\"en\":\"Latvia\",\"cn\":\"拉脱维亚\",\"es\":\"Letonia\",\"fr\":\"Lettonie\",\"ru\":\"Латвия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "NO",
                        Names =
                            "{\"ar\":\"النرويج\",\"en\":\"Norway\",\"cn\":\"挪威\",\"es\":\"Noruega\",\"fr\":\"Norvège\",\"ru\":\"Норвегия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "SJ",
                        Names =
                            "{\"ar\":\"جزيرتي سفالبارد وجان مايِن\",\"en\":\"Svalbard and Jan Mayen Islands\",\"cn\":\"斯瓦尔巴群岛和扬马延岛\",\"es\":\"Islas Svalbard y Jan Mayen\",\"fr\":\"Îles Svalbard-et-Jan Mayen\",\"ru\":\"Острова Свальбард и Ян-Майен\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "SE",
                        Names =
                            "{\"ar\":\"السويد\",\"en\":\"Sweden\",\"cn\":\"瑞典\",\"es\":\"Suecia\",\"fr\":\"Suède\",\"ru\":\"Швеция\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "CH",
                        Names =
                            "{\"ar\":\"سويسرا\",\"en\":\"Switzerland\",\"cn\":\"瑞士\",\"es\":\"Suiza\",\"fr\":\"Suisse\",\"ru\":\"Швейцария\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "DE",
                        Names =
                            "{\"ar\":\"ألمانيا\",\"en\":\"Germany\",\"cn\":\"德国\",\"es\":\"Alemania\",\"fr\":\"Allemagne\",\"ru\":\"Германия\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "FR",
                        Names =
                            "{\"ar\":\"فرنسا\",\"en\":\"France\",\"cn\":\"法国\",\"es\":\"Francia\",\"fr\":\"France\",\"ru\":\"Франция\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "LI",
                        Names =
                            "{\"ar\":\"ليختنشتاين\",\"en\":\"Liechtenstein\",\"cn\":\"列支敦士登\",\"es\":\"Liechtenstein\",\"fr\":\"Liechtenstein\",\"ru\":\"Лихтенштейн\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "LU",
                        Names =
                            "{\"ar\":\"لكسمبرغ\",\"en\":\"Luxembourg\",\"cn\":\"卢森堡\",\"es\":\"Luxemburgo\",\"fr\":\"Luxembourg\",\"ru\":\"Люксембург\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "MC",
                        Names =
                            "{\"ar\":\"موناكو\",\"en\":\"Monaco\",\"cn\":\"摩纳哥\",\"es\":\"Mónaco\",\"fr\":\"Monaco\",\"ru\":\"Монако\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "NL",
                        Names =
                            "{\"ar\":\"هولندا\",\"en\":\"Netherlands\",\"cn\":\"荷兰\",\"es\":\"Países Bajos\",\"fr\":\"Pays-Bas\",\"ru\":\"Нидерланды\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "GG",
                        Names =
                            "{\"ar\":\"غيرنسي\",\"en\":\"Guernsey\",\"cn\":\"格恩西\",\"es\":\"Guernsey\",\"fr\":\"Guernesey\",\"ru\":\"Гернси\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "JE",
                        Names =
                            "{\"ar\":\"جيرسي\",\"en\":\"Jersey\",\"cn\":\"泽西\",\"es\":\"Jersey\",\"fr\":\"Jersey\",\"ru\":\"Джерси\"}",
                        RegionId = 150
                    },
                    new Country
                    {
                        Code = "–",
                        Names =
                            "{\"ar\":\"سارك\",\"en\":\"Sark\",\"cn\":\"萨克\",\"es\":\"Sark\",\"fr\":\"Sercq\",\"ru\":\"Сарк\"}",
                        RegionId = 150
                    }
                );
        }
        
        private static void BuildRegion(ModelBuilder builder)
        {
            builder.Entity<Region>()
                .HasKey(c => c.Id);
            builder.Entity<Region>()
                .Property(c => c.Id)
                .IsRequired();
            builder.Entity<Region>()
                .Property(c => c.Names)
                .HasColumnType("jsonb")
                .IsRequired();
            builder.Entity<Region>()
                .HasData(
                    new Region
                    {
                        Id = 2,
                        Names =
                            "{\"ar\":\"أفريقيا\",\"cn\":\"非洲\",\"en\":\"Africa\",\"es\":\"África\",\"fr\":\"Afrique\",\"ru\":\"Африка\"}"
                    },
                    new Region
                    {
                        Id = 19,
                        Names =
                            "{\"ar\":\"الأمريكتان\",\"cn\":\"美洲\",\"en\":\"Americas\",\"es\":\"América\",\"fr\":\"Amérique\",\"ru\":\"Америка\"}"
                    },
                    new Region
                    {
                        Id = 142,
                        Names =
                            "{\"ar\":\"آسيا\",\"cn\":\"亚洲\",\"en\":\"Asia\",\"es\":\"Asia\",\"fr\":\"Asie\",\"ru\":\"Азия\"}"
                    },
                    new Region
                    {
                        Id = 150,
                        Names =
                            "{\"ar\":\"أوروبا\",\"cn\":\"欧洲\",\"en\":\"Europe\",\"es\":\"Europa\",\"fr\":\"Europe\",\"ru\":\"Европа\"}"
                    },
                    new Region
                    {
                        Id = 9,
                        Names =
                            "{\"ar\":\"أوقيانوسيا\",\"cn\":\"大洋洲\",\"en\":\"Oceania\",\"es\":\"Oceanía\",\"fr\":\"Océanie\",\"ru\":\"Океания\"}"
                    });
        }

        private void BuildCustomer(ModelBuilder builder)
        {
            builder.Entity<Customer>(customer =>
            {
                customer.HasKey(c => c.Id);
                customer.Property(c => c.Id).ValueGeneratedOnAdd();
                customer.Property(c => c.Email).IsRequired();
                customer.Property(c => c.Title).IsRequired();
                customer.Property(c => c.FirstName).IsRequired();
                customer.Property(c => c.LastName).IsRequired();
                customer.Property(c => c.FirstName).IsRequired();
                customer.Property(c => c.Position).IsRequired();
                customer.Property(c => c.IdentityHash).IsRequired();
            });
        }

        private void BuildCompany(ModelBuilder builder)
        {
            builder.Entity<Company>(company =>
            {
                company.HasKey(c => c.Id);
                company.Property(c => c.Id).ValueGeneratedOnAdd();
                company.Property(c => c.Address).IsRequired();
                company.Property(c => c.City).IsRequired();
                company.Property(c => c.CountryCode).IsRequired();
                company.Property(c => c.Name).IsRequired();
                company.Property(c => c.Phone).IsRequired();
                company.Property(c => c.PreferredCurrency).IsRequired();
                company.Property(c => c.PreferredPaymentMethod).IsRequired();
                company.Property(c => c.State).IsRequired();
            });
        }

        private void BuildCustomerCompanyRelation(ModelBuilder builder)
        {
            builder.Entity<CustomerCompanyRelation>(relation =>
            {
                relation.ToTable("CustomerCompanyRelations");
                
                relation.HasKey(r => new {r.CustomerId, r.CompanyId});
                relation.Property(r => r.CompanyId).IsRequired();
                relation.Property(r => r.CustomerId).IsRequired();
                relation.Property(r => r.Type).IsRequired();
            });
        }


        public DbSet<Country> Countries { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerCompanyRelation> CustomerCompanyRelations { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Region> Regions { get; set; }
    }
}
