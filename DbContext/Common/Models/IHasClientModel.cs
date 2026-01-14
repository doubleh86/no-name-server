namespace DbContext.Common.Models;

public interface IHasClientModel<out T> where T : class
{
    T ToClient();
}