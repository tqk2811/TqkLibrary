namespace TqkLibrary.Net.PhoneNumberApi.ChoThueSimCodeCom
{
  public class BaseResult<T>
  {
    public ResponseCode ResponseCode { get; set; }
    public string Msg { get; set; }
    public T Result { get; set; }
  }
}