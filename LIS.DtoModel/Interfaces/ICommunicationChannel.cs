using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LIS.DtoModel
{
    public interface ICommunicationChannel
    {
        Task<Token> Authenticate(string action, string token);
        
        /// <summary>
        /// Get request
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="headerParams">Header parameters</param>
        /// <param name="token">Authentication token</param>
        /// <returns>APIResponse</returns>
        Task<IAPIResponse> Get(string action, List<KeyValuePair<string, object>> headerParams = null, string token = null, CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        /// POST request
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="model">Model to send for post request</param>
        /// <param name="token">Authentication token</param>
        /// <returns>APIResponse</returns>
        Task<IAPIResponse> Post(string action, object model = null, string token = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// POST request
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="model">Model to send for post request</param>
        /// <param name="token">Authentication token</param>
        /// <returns>Generic Object</returns>
        Task<T> Post<T>(string action, object model = null);

        /// <summary>
        /// PUT request
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="model">Model to send for post request</param>
        /// <param name="token">Authentication token</param>
        /// <returns>APIResponse</returns>
        Task<IAPIResponse> Put(string action, object model = null, string token = null, CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        /// Delete request
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="token">Authentication token</param>
        Task<IAPIResponse> Delete(string action, string token = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
