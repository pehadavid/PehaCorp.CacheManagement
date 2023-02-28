using System;
using System.Threading.Tasks;

namespace PehaCorp.CacheManagement
{
    /// <summary>
    /// Interface de Gestion complète d'un système de cache
    /// </summary>
    public interface ICacheManager
    {

        /// <summary>
        /// Enregistre un objet dans le cache
        /// </summary>
        /// <param name="key">Clé de l'objet</param>
        /// <param name="value">Objet a sauvegarder</param>
        /// <param name="ttl">Durée de vie de l'objet dans le cache</param>
        void SetValue(string key, object value, TimeSpan? ttl);

        /// <summary>
        /// Efface une entrée du cache
        /// </summary>
        /// <param name="key">Clé de l'objet a supprimer</param>
        void Delete(string key);

        /// <summary>
        /// Efface une entrée du cache
        /// </summary>
        /// <param name="key"></param>
        Task<bool> DeleteAsync(string key);
        /// <summary>
        /// Vérifie si une clé correspond a un objet du cache
        /// </summary>
        /// <param name="key">Clé de l'objet a vérifier</param>
        /// <returns>Existance de l'objet dans le cache</returns>
        bool KeyExists(string key);

        /// <summary>
        /// Vide totalement le cache
        /// </summary>
        void Clear();

        /// <summary>
        /// Défini si le manager est activé ou pas.
        /// </summary>
        bool CacheEnabled { get; set; }

        /// <summary>
        /// Permet de rappatrier un objet du cache. Si l'objet n'est pas trouvé, la méthode passée en parametre est executée, l'objet est stocké en cache, puis renvoyé.
        /// </summary>
        /// <typeparam name="T">Type de l'objet attendu</typeparam>
        /// <param name="key">Clé de l'objet</param>
        /// <param name="DataRetrievalMethod">Méthode permettant de trouver l'objet si celui ci n'est pas présent dans le cache</param>
        /// <param name="TimeToLive">Durée de vie l'objet dans le cache</param>
        /// <returns>Un objet de type T, obtenu depuis le cache ou la méthode passée en parametre.</returns>
        T GetOrStore<T>(string key, Func<T> DataRetrievalMethod, TimeSpan? TimeToLive);

        /// <summary>
        /// Permet de rappatrier un object du cache, stocké dans un hashet. Si l'objet n'est pas trouvé, la méthode passée en parametre est executée, l'objet est stocké en cache, puis renvoyé.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashsetKey"></param>
        /// <param name="hashfieldKey"></param>
        /// <param name="DataRetrievalMethod"></param>
        /// <param name="ttl"></param>
        /// <returns></returns>
        T GetOrStore<T>(string hashsetKey, string hashfieldKey, Func<T> DataRetrievalMethod, TimeSpan? ttl);
        /// <summary>
        /// Permet de rapatrier un objet du cache de manière asynchrone. Si l'objet n'est pas trouvé, la tache asynchrone passée en parametre est executée, l'objet est stocké en cache, puis renvoyé
        /// </summary>
        /// <typeparam name="T">Type de l'objet attendu</typeparam>
        /// <param name="key">clé de l'objet</param>
        /// <param name="dataRetrievalMethodAsync"> Tache asynchrone de génération de donnée</param>
        /// <param name="timeToLive">Durée de vie de l'objet dans le cache</param>
        /// <returns></returns>
        Task<T> GetOrStoreAsync<T>(string key, Func<Task<T>> dataRetrievalMethodAsync, TimeSpan? timeToLive);

        T HashGetByKey<T>(string hashsetKey, string itemKey);

        void HashSetByKey<T>(string hashsetKey, string itemKey, T item);

        void DeleteHashSet(string hashsetKey);

    }
}
