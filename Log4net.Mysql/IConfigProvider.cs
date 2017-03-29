using log4net;
using System;
using System.Collections.Generic;
using System.IO;

namespace Log4net.Mysql
{
    public interface ILog4netConfigProvider
    {
        bool Accessable(Type type);
        IEnumerable<Log4netConfigDataSource> GetConfigDataSources();
    }

    public class DefalutConfigProvider : ILog4netConfigProvider
    {
        public DefalutConfigProvider()
        {
            _configDataSources = GetConfigDataSources();
        }

        private IEnumerable<Log4netConfigDataSource> _configDataSources;

        /// <summary>
        /// 指定能获取的源，一般是优先 程序目录下，再D盘，再远程服务去取，最后从备份里取
        /// </summary>
        /// <returns>源列表，请注意顺序</returns>
        public virtual IEnumerable<Log4netConfigDataSource> GetConfigDataSources()
        {
            return new Log4netConfigDataSource[] {
                    new AppDomainConfigDataSource()
                    };
        }

        private ILog _logger;
        public virtual ILog Logger
        {
            get
            {
                if (_logger == null || !_logger.IsErrorEnabled)
                {
                    _logger = log4net.LogManager.GetLogger("ConfigLog");
                }

                return _logger;
            }
        }

        /// <summary>
        /// 判断是否有访问此配置的权限
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual bool Accessable(Type type)
        {
            return true;
        }

        public virtual T Get<T>() where T : class
        {
            T result = null;

            var type = typeof(T);

            //不同源取配置，然后反序列化，然后缓存起来
            if (!Accessable(type))
            {
                //先不抛异常，几条日志
                Logger.WarnFormat("获取配置[{0}]警告，[{1}]没有权限访问此配置！", typeof(T));
            }

            //按顺序，去不同源去取配置，一般是优先 程序目录下，再D盘，再远程服务去取
            string content = string.Empty;
            string filePath = string.Empty;

            foreach (var ds in _configDataSources)
            {
                content = ds.GetConfigContent(type);
                if (!string.IsNullOrEmpty(content))
                {
                    //如果是本地Config，且不是备份Config，需读取filePath以做缓存文件依赖
                    filePath = ((LocalConfigDataSource)ds).GetFilePath(type);

                    break;
                }
            }

            if (!string.IsNullOrEmpty(content))
            {
                //if (type.BaseType == typeof(ConfigUnitForContent))
                //{
                //    var configUnit = (ConfigUnitForContent)Activator.CreateInstance(type);
                //    configUnit.Content = content;

                //    result = configUnit as T;
                //}
                //else
                //{
                //    result = (T)SerializationHelper.XmlDeserialize(typeof(T), content);
                //}
                result = (T)SerializationHelper.XmlDeserialize(typeof(T), content);
            }

            return result;
        }
    }

    public class LocalConfigProvider : DefalutConfigProvider
    {
        public override IEnumerable<Log4netConfigDataSource> GetConfigDataSources()
        {
            return new Log4netConfigDataSource[] {
                    new AppDomainConfigDataSource()
            };
        }
    }

    public abstract class Log4netConfigDataSource
    {
        public const string MainHardDisks = "D";
        public abstract string GetConfigContent(Type t);

        private ILog _logger;
        public virtual ILog Logger
        {
            get
            {
                if (_logger == null || !_logger.IsErrorEnabled)
                {
                    _logger = LogManager.GetLogger("ConfigLog");
                }

                return _logger;
            }
        }
    }

    public abstract class LocalConfigDataSource : Log4netConfigDataSource
    {
        public abstract string RootPath { get; }

        public override string GetConfigContent(Type t)
        {
            var configFile = GetFilePath(t);
            if (File.Exists(configFile))
            {
                return File.ReadAllText(configFile);
            }
            else
            {
                return string.Empty;
            }
        }

        public virtual string GetFilePath(Type t)
        {
            return Path.Combine(RootPath, t.Name + ".xml");
        }
    }

    /// <summary>
    /// 访问程序目录下如Configs读取配置，对当前程序有效
    /// </summary>
    public class AppDomainConfigDataSource : LocalConfigDataSource
    {
        public override string RootPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");
            }
        }

        public AppDomainConfigDataSource()
        {
        }
    }
}
