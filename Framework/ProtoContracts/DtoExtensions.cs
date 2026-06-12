using System;
using System.Collections.Generic;
using System.Linq;

namespace Tecan.Sila2
{
    /// <summary>
    /// Helper class with extensions for Dtos
    /// </summary>
    public static class DtoExtensions
    {
        /// <summary>
        /// Tries to extract the given data transfer objects
        /// </summary>
        /// <typeparam name="T">The type of the encapsulated object</typeparam>
        /// <param name="transferObject">The DTO object</param>
        /// <param name="resolver">The binary resolver</param>
        /// <param name="parameterName">The name of the parameter in case of exceptions</param>
        /// <returns>The inner object</returns>
        /// <exception cref="ArgumentException">Raised if something goes wrong during extraction</exception>
        public static T TryExtract<T>( this ISilaTransferObject<T> transferObject, IBinaryStore resolver, string parameterName )
        {
            try
            {
                return transferObject.Extract( resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Tries to extract the given data transfer objects
        /// </summary>
        /// <typeparam name="T">The type of the encapsulated object</typeparam>
        /// <param name="transferObject">The DTO object</param>
        /// <param name="resolver">The binary resolver</param>
        /// <param name="parameterName">The name of the parameter in case of exceptions</param>
        /// <returns>The inner object</returns>
        /// <exception cref="ArgumentException">Raised if something goes wrong during extraction</exception>
        public static List<T> TryExtract<T>( this IEnumerable<ISilaTransferObject<T>> transferObject, IBinaryStore resolver, string parameterName )
        {
            try
            {
                return transferObject.Extract( resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Tries to extract the given data transfer objects into a dictionary
        /// </summary>
        /// <typeparam name="TKey">The key type of the dictionary</typeparam>
        /// <typeparam name="TKeyDto">The data transfer object type for the key</typeparam>
        /// <typeparam name="TValue">The value type of the dictionary</typeparam>
        /// <typeparam name="TValueDto">The data transfer object type for the value</typeparam>
        /// <param name="source">The DTO representation of the dictionary</param>
        /// <param name="resolver">The binary resolver</param>
        /// <param name="parameterName">The name of the parameter in case of exceptions</param>
        /// <returns>The inner object</returns>
        /// <exception cref="ArgumentException">Raised if something goes wrong during extraction</exception>
        public static Dictionary<TKey, TValue> TryExtract<TKey, TKeyDto, TValue, TValueDto>( this IEnumerable<KeyValuePairDto<TKey, TKeyDto, TValue, TValueDto>> source,
            IBinaryStore resolver, string parameterName )
            where TKeyDto : ISilaTransferObject<TKey>
            where TValueDto : ISilaTransferObject<TValue>
        {
            try
            {
                return source.Extract( resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Tries to extract the given data transfer objects
        /// </summary>
        /// <typeparam name="T">The type of the encapsulated object</typeparam>
        /// <param name="transferObject">The DTO object</param>
        /// <param name="resolver">The binary resolver</param>
        /// <param name="parameterName">The name of the parameter in case of exceptions</param>
        /// <returns>The inner object</returns>
        /// <exception cref="ArgumentException">Raised if something goes wrong during extraction</exception>
        public static T[] TryExtractToArray<T>( this IEnumerable<ISilaTransferObject<T>> transferObject, IBinaryStore resolver, string parameterName )
        {
            try
            {
                return transferObject.ExtractToArray( resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Extracts the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="T">The type of objects</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>A collection with the underlying data</returns>
        public static List<T> Extract<T>( this IEnumerable<ISilaTransferObject<T>> source, IBinaryStore resolver )
        {
            return source?.Select( it => it.Extract( resolver ) ).ToList();
        }

        /// <summary>
        /// Extracts the collection of key-value pairs in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="TKey">The domain type of the key</typeparam>
        /// <typeparam name="TKeyDto">The data transfer type of the key</typeparam>
        /// <typeparam name="TValue">The domain type of the value</typeparam>
        /// <typeparam name="TValueDto">The data transfer type of the value</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>A dictionary with the underlying data</returns>
        public static Dictionary<TKey, TValue> Extract<TKey, TKeyDto, TValue, TValueDto>(this IEnumerable<KeyValuePairDto<TKey, TKeyDto, TValue, TValueDto>> source,
            IBinaryStore resolver)
            where TKeyDto : ISilaTransferObject<TKey>
            where TValueDto : ISilaTransferObject<TValue>
        {
            if (source == null)
            {
                return null;
            }
            var dict = new Dictionary<TKey, TValue>();
            foreach (var keyValuePairDto in source)
            {
                var pair = keyValuePairDto.Extract(resolver);
                dict.Add(pair.Key, pair.Value);
            }

            return dict;
        }

        /// <summary>
        /// Extracts the collection of key-value pairs in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="TKey">The domain type of the key</typeparam>
        /// <typeparam name="TKeyDto">The data transfer type of the key</typeparam>
        /// <typeparam name="TValue">The domain type of the value</typeparam>
        /// <typeparam name="TValueDto">The data transfer type of the value</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>A dictionary with the underlying data</returns>
        public static Dictionary<TKey, IReadOnlyList<TValue>> Extract<TKey, TKeyDto, TValue, TValueDto>(this IEnumerable<LookupEntryDto<TKey, TKeyDto, TValue, TValueDto>> source,
            IBinaryStore resolver)
            where TKeyDto : ISilaTransferObject<TKey>
            where TValueDto : class, ISilaTransferObject<TValue>
        {
            if (source == null)
            {
                return null;
            }
            var dict = new Dictionary<TKey, IReadOnlyList<TValue>>();
            foreach (var keyValuePairDto in source)
            {
                var pair = keyValuePairDto.Extract(resolver);
                dict.Add(pair.Key, pair.Value);
            }

            return dict;
        }

        /// <summary>
        /// Extracts the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="TSource">The type of objects</typeparam>
        /// <typeparam name="TConverted">The type of the converted data</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>A collection with the underlying data</returns>
        /// <remarks>This only works if the types <typeparamref name="TSource"/> and <typeparamref name="TConverted"/> are compatible.</remarks>
        public static List<TConverted> ExtractConverted<TSource, TConverted>( this IEnumerable<ISilaTransferObject<TSource>> source, IBinaryStore resolver )
        {
            return source?.Select( it => (TConverted)Convert.ChangeType( it.Extract( resolver ), typeof( TConverted ) ) ).ToList();
        }

        /// <summary>
        /// Tries to extract the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="TSource">The type of objects</typeparam>
        /// <typeparam name="TConverted">The type of the converted data</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <param name="parameterName">The name of the parameter in case of exceptions</param>
        /// <returns>A collection with the underlying data</returns>
        /// <remarks>This only works if the types <typeparamref name="TSource"/> and <typeparamref name="TConverted"/> are compatible.</remarks>
        public static List<TConverted> TryExtractConverted<TSource, TConverted>( this IEnumerable<ISilaTransferObject<TSource>> source, IBinaryStore resolver, string parameterName )
        {
            try
            {
                return ExtractConverted<TSource, TConverted>( source, resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Extracts the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="T">The type of objects</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>An array with the underlying data</returns>
        public static T[] ExtractToArray<T>( this IEnumerable<ISilaTransferObject<T>> source, IBinaryStore resolver )
        {
            return source?.Select( it => it.Extract( resolver ) ).ToArray();
        }

        /// <summary>
        /// Extracts the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="TSource">The type of objects</typeparam>
        /// <typeparam name="TConverted">The type of the converted data</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <returns>A collection with the underlying data</returns>
        /// <remarks>This only works if the types <typeparamref name="TSource"/> and <typeparamref name="TConverted"/> are compatible.</remarks>
        public static TConverted[] ExtractConvertedToArray<TSource, TConverted>( this IEnumerable<ISilaTransferObject<TSource>> source, IBinaryStore resolver )
        {
            return source?.Select( it => (TConverted)Convert.ChangeType( it.Extract( resolver ), typeof( TConverted ) ) ).ToArray();
        }

        /// <summary>
        /// Tries to extract the collection of values in the collection of data transfer objects
        /// </summary>
        /// <typeparam name="TSource">The type of objects</typeparam>
        /// <typeparam name="TConverted">The type of the converted data</typeparam>
        /// <param name="source">A collection with data transfer objects</param>
        /// <param name="resolver">A component that resolves binary storage identifiers</param>
        /// <param name="parameterName">The name of the parameter in case of exceptions</param>
        /// <returns>A collection with the underlying data</returns>
        /// <remarks>This only works if the types <typeparamref name="TSource"/> and <typeparamref name="TConverted"/> are compatible.</remarks>
        public static TConverted[] TryExtractConvertedToArray<TSource, TConverted>( this IEnumerable<ISilaTransferObject<TSource>> source, IBinaryStore resolver, string parameterName )
        {
            try
            {
                return ExtractConvertedToArray<TSource, TConverted>( source, resolver );
            }
            catch(Exception ex)
            {
                throw new ArgumentException( ex.Message, parameterName, ex );
            }
        }

        /// <summary>
        /// Encapsulates the given collection into data transfer objects
        /// </summary>
        /// <typeparam name="TDto">The type of data transfer objects</typeparam>
        /// <typeparam name="TInner">The type of data</typeparam>
        /// <param name="source">The collection of data objects</param>
        /// <param name="convertFunc">A conversion function to encapsulate the data</param>
        /// <param name="resolver">A component to handle binary data transfer</param>
        /// <returns></returns>
        public static List<TDto> Encapsulate<TDto, TInner>( IEnumerable<TInner> source,
            Func<TInner, IBinaryStore, TDto> convertFunc, IBinaryStore resolver )
        {
            return source?.Select( item => convertFunc( item, resolver ) ).ToList();
        }

        /// <summary>
        /// Encapsulates the given collection into data transfer objects
        /// </summary>
        /// <typeparam name="TKey">The type of the keys</typeparam>
        /// <typeparam name="TKeyDto">The data transfer type for the keys</typeparam>
        /// <typeparam name="TValue">The type of the values</typeparam>
        /// <typeparam name="TValueDto">The data transfer type of the values</typeparam>
        /// <param name="source">The collection of data objects</param>
        /// <param name="keyConvertFunc">A function that converts the keys into their data transfer objects</param>
        /// <param name="valueConvertFunc">A function that converts the values into their data transfer objects</param>
        /// <param name="resolver">A component to handle binary data transfer</param>
        /// <returns></returns>
        public static List<KeyValuePairDto<TKey, TKeyDto, TValue, TValueDto>> Encapsulate<TKeyDto, TKey, TValueDto, TValue>( IEnumerable<KeyValuePair<TKey, TValue>> source,
            Func<TKey, IBinaryStore, TKeyDto> keyConvertFunc, Func<TValue, IBinaryStore, TValueDto> valueConvertFunc, IBinaryStore resolver )
            where TKeyDto : ISilaTransferObject<TKey>
            where TValueDto : ISilaTransferObject<TValue>
        {
            return source?.Select( item => new KeyValuePairDto<TKey, TKeyDto, TValue, TValueDto>()
            {
                Key = keyConvertFunc( item.Key, resolver ),
                Value = valueConvertFunc( item.Value, resolver )
            } ).ToList();
        }

        /// <summary>
        /// Encapsulates the given collection into data transfer objects
        /// </summary>
        /// <typeparam name="TKey">The type of the keys</typeparam>
        /// <typeparam name="TKeyDto">The data transfer type for the keys</typeparam>
        /// <typeparam name="TValue">The type of the values</typeparam>
        /// <typeparam name="TValueDto">The data transfer type of the values</typeparam>
        /// <param name="source">The collection of data objects</param>
        /// <param name="keyConvertFunc">A function that converts the keys into their data transfer objects</param>
        /// <param name="valueConvertFunc">A function that converts the values into their data transfer objects</param>
        /// <param name="resolver">A component to handle binary data transfer</param>
        /// <returns></returns>
        public static List<LookupEntryDto<TKey, TKeyDto, TValue, TValueDto>> Encapsulate<TKeyDto, TKey, TValueDto, TValue>( IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> source,
            Func<TKey, IBinaryStore, TKeyDto> keyConvertFunc, Func<TValue, IBinaryStore, TValueDto> valueConvertFunc, IBinaryStore resolver )
            where TKeyDto : ISilaTransferObject<TKey>
            where TValueDto : class, ISilaTransferObject<TValue>
        {
            return source?.Select( item => new LookupEntryDto<TKey, TKeyDto, TValue, TValueDto>()
            {
                Key = keyConvertFunc( item.Key, resolver ),
                Value = Encapsulate( item.Value, valueConvertFunc, resolver )
            } ).ToList();
        }
    }
}
