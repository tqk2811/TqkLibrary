using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

//using System.Windows.Forms;

namespace TqkLibrary
{
  public static class LibExtensions
  {
    private static readonly Random rd = new Random();

    public static T RandomRemove<T>(this List<T> list)
    {
      T t = list[rd.Next(0, list.Count - 1)];
      list.Remove(t);
      return t;
    }

    public static string RandomString(int length)
    {
      const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
      return new string(Enumerable.Repeat(chars, length).Select(s => s[rd.Next(s.Length)]).ToArray());
    }

    //public static OpenFileDialog InitOpenFileDialog(
    //  string Filter = "All files (*.*)|*.*",
    //  string InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
    //  bool Multiselect = true)
    //{
    //  OpenFileDialog openFileDialog = new OpenFileDialog();
    //  openFileDialog.InitialDirectory = InitialDirectory;//Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
    //  openFileDialog.Filter = Filter;
    //  openFileDialog.FilterIndex = 0;
    //  openFileDialog.CheckFileExists = true;
    //  openFileDialog.Multiselect = Multiselect;
    //  return openFileDialog;
    //}

    private static readonly Regex regex_IsCombiningDiacriticalMarks = new Regex("\\p{IsCombiningDiacriticalMarks}+");

    public static string convertToUnSign3(this string s)
    {
      string temp = s.Normalize(NormalizationForm.FormD);
      return regex_IsCombiningDiacriticalMarks.Replace(temp, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D').Replace(" ", "-");
    }
  }
}