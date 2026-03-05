using System;

namespace _Project.Scripts.Infrastructure.Services.Config
{
    public interface IGameConfig
    {
    }

    public interface IConfigCategoryProvider
    {
        string Category { get; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GameConfigAttribute : Attribute
    {
        public string Category { get; }

        public GameConfigAttribute(string category = null)
        {
            Category = category;
        }
    }
}
