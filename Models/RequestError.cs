namespace ComiServ.Models
{
    public class RequestError
    {

        public static RequestError InvalidHandle => new("Invalid handle");
        public static RequestError ComicNotFound => new("Comic not found");
        public static RequestError CoverNotFound => new("Cover not found");
        public static RequestError PageNotFound => new("Page not found");
        public static RequestError FileNotFound => new("File not found");
        public string[] Errors { get; }
        public RequestError(string ErrorMessage)
        {
            Errors = [ErrorMessage];
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
            return new RequestError(Errors.Concat(other))
                ;
        }
    }
}
