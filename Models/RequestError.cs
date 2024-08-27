using System.Collections;

namespace ComiServ.Models
{
    public class RequestError : IEnumerable<string>
    {
        public static RequestError InvalidHandle => new("Invalid handle");
        public static RequestError ComicNotFound => new("Comic not found");
        public static RequestError CoverNotFound => new("Cover not found");
        public static RequestError PageNotFound => new("Page not found");
        public static RequestError FileNotFound => new("File not found");
        public static RequestError ThumbnailNotFound => new("Thumbnail not found");
        public static RequestError NotAuthenticated => new("Not authenticated");
        public static RequestError NoAccess => new("User does not have access to this resource");
        public static RequestError UserNotFound => new("User not found");
        public static RequestError ComicFileExists => new("Comic file exists so comic not deleted");
        public static RequestError UserSpecificEndpoint => new("Endpoint is user-specific, requires login");
        public string[] Errors { get; }
        public RequestError(string ErrorMessage)
        {
            Errors = [ErrorMessage];
        }
        public RequestError()
        {
            Errors = [];
        }
        public RequestError(IEnumerable<string> ErrorMessages)
        {
            Errors = ErrorMessages.ToArray();
        }
        public RequestError And(RequestError other)
        {
            return new RequestError(Errors.Concat(other.Errors));
        }
        public RequestError And(string other)
        {
            return new RequestError(Errors.Append(other));
        }
        public RequestError And(IEnumerable<string> other)
        {
            return new RequestError(Errors.Concat(other));
        }
        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)Errors).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
