using ABServer.Model.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ABServer
{
    /// <summary>
    /// Организует работу с пользователями
    /// </summary>
    class UsersManager
    {
        public static event Action BaseUpdate;

        public UsersManager()
        {
            if (!IsLoad)
                Load();
        }

        //Были произведенна загрузка
        private static bool IsLoad = false;

        static List<User> _userList;

        //Объект блокировки потоков
        private static object obj = new object();

        public List<User> GetAllUser()
        {
            return _userList;
        }

        public bool CheckAccess(string Login, string Password)
        {
            var rez = _userList.Where(x => x.Login == Login && x.Password == Password).ToList();
            if (rez.Count != 1)
            {
                throw new ArgumentException($"Пользователя с логином: {Login} и паролем: {Password} не найденно");
            }
            return true;
        }

        public User MakeAuth(string Login, string Password)
        {
            var rez = _userList.FirstOrDefault(x => x.Login == Login && x.Password == Password);
            if (rez == null)
            {
                throw new ArgumentException($"Пользователя с логином: {Login} и паролем: {Password} не найденно");
            }

            return rez;
        }

        public void Save()
        {
            try
            {
                if (_userList == null)
                    throw new ArgumentNullException("База не загружена!");
                lock (obj)
                {
                    var mr = new MemoryStream();
                    BinaryFormatter fr = new BinaryFormatter();
                    fr.Serialize(mr, _userList);
                    File.WriteAllBytes("users.dat", mr.ToArray());
                    mr.Close();
                }

                if (BaseUpdate != null)
                    BaseUpdate();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Неудалось сохранить базу упользователей. и вот почему: " + ex.Message);

            }
        }

        private void Load()
        {
            try
            {
                lock (obj)
                {
                    BinaryFormatter fr = new BinaryFormatter();
                    var file = File.OpenRead("users.dat");
                    _userList = (List<User>)fr.Deserialize(file);
                    IsLoad = true;
                    file.Close();
                }
            }
            catch
            {
                lock (obj)
                {
                    _userList = new List<User>();
                    IsLoad = true;
                }
            }
        }

        public void AddUser(User User)
        {
            _userList.Add(User);
            Save();

        }

        public void UpdateUser(User userOld, User userNew)
        {
            var index = _userList.IndexOf(userOld);
            _userList[index] = userNew;
            Save();
        }

        public void UpdateUser(User userNew)
        {
            for (int i = 0; i < _userList.Count; i++)
            {
                if (_userList[i].Login == userNew.Login)
                {
                    _userList[i] = userNew;
                    Save();
                    return;
                }
            }
        }

        public User GetUser(string Login)
        {
            return _userList.Where(x => x.Login == Login).First();
        }

        public void UserDelete(User user)
        {
            _userList.Remove(user);
            Save();
        }
    }
}
