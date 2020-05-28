// ***********************************************************************
// Assembly         : UltraDES
// Author           : Lucas Alves
// Created          : 04-20-2020
//
// Last Modified By : Lucas Alves
// Last Modified On : 04-20-2020


using System;

namespace UltraDES
{
    
    /// <summary>
    /// Interface for option.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <remarks>Lucas Alves, 18/01/2016.</remarks>
    

    public interface Option<T>
    {
        
        /// <summary>
        /// Gets a value indicating whether this object is some.
        /// </summary>
        /// <value>true if this object is some, false if not.</value>
        

        bool IsSome { get; }

        
        /// <summary>
        /// Gets a value indicating whether this object is none.
        /// </summary>
        /// <value>true if this object is none, false if not.</value>
        

        bool IsNone { get; }

        
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        

        T Value { get; }
    }

    
    /// <summary>
    /// (Serializable)a some.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <remarks>Lucas Alves, 18/01/2016.</remarks>
    

    [Serializable]
    public class Some<T> : Option<T>
    {
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Lucas Alves, 18/01/2016.</remarks>
        

        private Some(T value) => Value = value;


        /// <summary>
        /// Gets a value indicating whether this object is none.
        /// </summary>
        /// <value>true if this object is none, false if not.</value>
        

        public bool IsNone => false;


        /// <summary>
        /// Gets a value indicating whether this object is some.
        /// </summary>
        /// <value>true if this object is some, false if not.</value>
        

        public bool IsSome => true;


        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        

        public T Value { get; private set; }

        
        /// <summary>
        /// Creates a new Option&lt;T&gt;
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>An Option&lt;T&gt;</returns>
        /// <remarks>Lucas Alves, 18/01/2016.</remarks>
        

        public static Option<T> Create(T value) => new Some<T>(value);
    }

    
    /// <summary>
    /// (Serializable)a none.
    /// </summary>
    /// <typeparam name="T">Generic type parameter.</typeparam>
    /// <remarks>Lucas Alves, 18/01/2016.</remarks>
    

    [Serializable]
    public class None<T> : Option<T>
    {
        /// <summary>
        /// The singleton.
        /// </summary>
        private static readonly None<T> Singleton = new None<T>();

        
        /// <summary>
        /// Constructor that prevents a default instance of this class from being created.
        /// </summary>
        /// <remarks>Lucas Alves, 18/01/2016.</remarks>
        

        private None() { }

        
        /// <summary>
        /// Gets a value indicating whether this object is none.
        /// </summary>
        /// <value>true if this object is none, false if not.</value>
        

        public bool IsNone => true;


        /// <summary>
        /// Gets a value indicating whether this object is some.
        /// </summary>
        /// <value>true if this object is some, false if not.</value>
        

        public bool IsSome => false;


        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        

        public T Value => default(T);


        /// <summary>
        /// Creates a new Option&lt;T&gt;
        /// </summary>
        /// <returns>An Option&lt;T&gt;</returns>
        /// <remarks>Lucas Alves, 18/01/2016.</remarks>
        

        public static Option<T> Create() => Singleton;
    }
}