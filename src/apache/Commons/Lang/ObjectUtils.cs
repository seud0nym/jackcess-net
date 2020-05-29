/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Sharpen;
using System;
using System.Text;

namespace Apache.Commons.Lang
{
    /// <summary>
    /// <p>Operations on <code>Object</code>.</p>
    /// <p>This class tries to handle <code>null</code> input gracefully.
    /// </summary>
    /// <remarks>
    /// <p>Operations on <code>Object</code>.</p>
    /// <p>This class tries to handle <code>null</code> input gracefully.
    /// An exception will generally not be thrown for a <code>null</code> input.
    /// Each method documents its behaviour in more detail.</p>
    /// </remarks>
    /// <author>Apache Software Foundation</author>
    /// <author><a href="mailto:nissim@nksystems.com">Nissim Karpenstein</a></author>
    /// <author><a href="mailto:janekdb@yahoo.co.uk">Janek Bogucki</a></author>
    /// <author>Daniel L. Rall</author>
    /// <author>Gary Gregory</author>
    /// <author>Mario Winterer</author>
    /// <author><a href="mailto:david@davidkarlsen.com">David J. M. Karlsen</a></author>
    /// <since>1.0</since>
    /// <version>$Id: ObjectUtils.java 905636 2010-02-02 14:03:32Z niallp $</version>
    public class ObjectUtils
    {
        /// <summary>
        /// <p>Singleton used as a <code>null</code> placeholder where
        /// <code>null</code> has another meaning.</p>
        /// <p>For example, in a <code>HashMap</code> the
        /// <see cref="System.Collections.Hashtable{K, V}.Get(object)">System.Collections.Hashtable&lt;K, V&gt;.Get(object)
        /// 	</see>
        /// method returns
        /// <code>null</code> if the <code>Map</code> contains
        /// <code>null</code> or if there is no matching key. The
        /// <code>Null</code> placeholder can be used to distinguish between
        /// these two cases.</p>
        /// <p>Another example is <code>Hashtable</code>, where <code>null</code>
        /// cannot be stored.</p>
        /// <p>This instance is Serializable.</p>
        /// </summary>
        public static readonly ObjectUtils.Null NULL = new ObjectUtils.Null();

        /// <summary>
        /// <p><code>ObjectUtils</code> instances should NOT be constructed in
        /// standard programming.
        /// </summary>
        /// <remarks>
        /// <p><code>ObjectUtils</code> instances should NOT be constructed in
        /// standard programming. Instead, the class should be used as
        /// <code>ObjectUtils.defaultIfNull("a","b");</code>.</p>
        /// <p>This constructor is public to permit tools that require a JavaBean instance
        /// to operate.</p>
        /// </remarks>
        public ObjectUtils() : base()
        {
        }

        // Defaulting
        //-----------------------------------------------------------------------
        /// <summary>
        /// <p>Returns a default value if the object passed is
        /// <code>null</code>.</p>
        /// <pre>
        /// ObjectUtils.defaultIfNull(null, null)      = null
        /// ObjectUtils.defaultIfNull(null, "")        = ""
        /// ObjectUtils.defaultIfNull(null, "zz")      = "zz"
        /// ObjectUtils.defaultIfNull("abc", *)        = "abc"
        /// ObjectUtils.defaultIfNull(Boolean.TRUE, *) = Boolean.TRUE
        /// </pre>
        /// </summary>
        /// <param name="object">the <code>Object</code> to test, may be <code>null</code></param>
        /// <param name="defaultValue">the default value to return, may be <code>null</code></param>
        /// <returns><code>object</code> if it is not <code>null</code>, defaultValue otherwise
        /// 	</returns>
        public static object DefaultIfNull(object @object, object defaultValue)
        {
            return @object != null ? @object : defaultValue;
        }

        /// <summary>
        /// <p>Compares two objects for equality, where either one or both
        /// objects may be <code>null</code>.</p>
        /// <pre>
        /// ObjectUtils.equals(null, null)                  = true
        /// ObjectUtils.equals(null, "")                    = false
        /// ObjectUtils.equals("", null)                    = false
        /// ObjectUtils.equals("", "")                      = true
        /// ObjectUtils.equals(Boolean.TRUE, null)          = false
        /// ObjectUtils.equals(Boolean.TRUE, "true")        = false
        /// ObjectUtils.equals(Boolean.TRUE, Boolean.TRUE)  = true
        /// ObjectUtils.equals(Boolean.TRUE, Boolean.FALSE) = false
        /// </pre>
        /// </summary>
        /// <param name="object1">the first object, may be <code>null</code></param>
        /// <param name="object2">the second object, may be <code>null</code></param>
        /// <returns><code>true</code> if the values of both objects are the same</returns>
        public new static bool Equals(object object1, object object2)
        {
            if (object1 == object2)
            {
                return true;
            }
            if ((object1 == null) || (object2 == null))
            {
                return false;
            }
            return object1.Equals(object2);
        }

        /// <summary>
        /// <p>Gets the hash code of an object returning zero when the
        /// object is <code>null</code>.</p>
        /// <pre>
        /// ObjectUtils.hashCode(null)   = 0
        /// ObjectUtils.hashCode(obj)    = obj.hashCode()
        /// </pre>
        /// </summary>
        /// <param name="obj">the object to obtain the hash code of, may be <code>null</code>
        /// 	</param>
        /// <returns>the hash code of the object, or zero if null</returns>
        /// <since>2.1</since>
        public static int HashCode(object obj)
        {
            return (obj == null) ? 0 : obj.GetHashCode();
        }

        // Identity ToString
        //-----------------------------------------------------------------------
        /// <summary>
        /// <p>Gets the toString that would be produced by <code>Object</code>
        /// if a class did not override toString itself.
        /// </summary>
        /// <remarks>
        /// <p>Gets the toString that would be produced by <code>Object</code>
        /// if a class did not override toString itself. <code>null</code>
        /// will return <code>null</code>.</p>
        /// <pre>
        /// ObjectUtils.identityToString(null)         = null
        /// ObjectUtils.identityToString("")           = "java.lang.String@1e23"
        /// ObjectUtils.identityToString(Boolean.TRUE) = "java.lang.Boolean@7fa"
        /// </pre>
        /// </remarks>
        /// <param name="object">
        /// the object to create a toString for, may be
        /// <code>null</code>
        /// </param>
        /// <returns>
        /// the default toString text, or <code>null</code> if
        /// <code>null</code> passed in
        /// </returns>
        public static string IdentityToString(object @object)
        {
            if (@object == null)
            {
                return null;
            }
            StringBuilder buffer = new StringBuilder();
            IdentityToString(buffer, @object);
            return buffer.ToString();
        }

        /// <summary>
        /// <p>Appends the toString that would be produced by <code>Object</code>
        /// if a class did not override toString itself.
        /// </summary>
        /// <remarks>
        /// <p>Appends the toString that would be produced by <code>Object</code>
        /// if a class did not override toString itself. <code>null</code>
        /// will throw a NullPointerException for either of the two parameters. </p>
        /// <pre>
        /// ObjectUtils.identityToString(buf, "")            = buf.append("java.lang.String@1e23"
        /// ObjectUtils.identityToString(buf, Boolean.TRUE)  = buf.append("java.lang.Boolean@7fa"
        /// ObjectUtils.identityToString(buf, Boolean.TRUE)  = buf.append("java.lang.Boolean@7fa")
        /// </pre>
        /// </remarks>
        /// <param name="buffer">the buffer to append to</param>
        /// <param name="object">the object to create a toString for</param>
        /// <since>2.4</since>
        public static void IdentityToString(StringBuilder buffer, object @object)
        {
            if (@object == null)
            {
                throw new ArgumentNullException("Cannot get the toString of a null identity");
            }
            buffer.Append(@object.GetType().FullName).Append('@').Append(Sharpen.Extensions.ToHexString
                (Runtime.IdentityHashCode(@object)));
        }

        /// <summary>
        /// <p>Appends the toString that would be produced by <code>Object</code>
        /// if a class did not override toString itself.
        /// </summary>
        /// <remarks>
        /// <p>Appends the toString that would be produced by <code>Object</code>
        /// if a class did not override toString itself. <code>null</code>
        /// will return <code>null</code>.</p>
        /// <pre>
        /// ObjectUtils.appendIdentityToString(*, null)            = null
        /// ObjectUtils.appendIdentityToString(null, "")           = "java.lang.String@1e23"
        /// ObjectUtils.appendIdentityToString(null, Boolean.TRUE) = "java.lang.Boolean@7fa"
        /// ObjectUtils.appendIdentityToString(buf, Boolean.TRUE)  = buf.append("java.lang.Boolean@7fa")
        /// </pre>
        /// </remarks>
        /// <param name="buffer">the buffer to append to, may be <code>null</code></param>
        /// <param name="object">the object to create a toString for, may be <code>null</code>
        /// 	</param>
        /// <returns>
        /// the default toString text, or <code>null</code> if
        /// <code>null</code> passed in
        /// </returns>
        /// <since>2.0</since>
        [System.ObsoleteAttribute(@"The design of this method is bad - see LANG-360. Instead, use identityToString(StringBuffer, Object)."
            )]
        public static StringBuilder AppendIdentityToString(StringBuilder buffer, object @object
            )
        {
            if (@object == null)
            {
                return null;
            }
            if (buffer == null)
            {
                buffer = new StringBuilder();
            }
            return buffer.Append(@object.GetType().FullName).Append('@').Append(Sharpen.Extensions.ToHexString
                (Runtime.IdentityHashCode(@object)));
        }

        // ToString
        //-----------------------------------------------------------------------
        /// <summary>
        /// <p>Gets the <code>toString</code> of an <code>Object</code> returning
        /// an empty string ("") if <code>null</code> input.</p>
        /// <pre>
        /// ObjectUtils.toString(null)         = ""
        /// ObjectUtils.toString("")           = ""
        /// ObjectUtils.toString("bat")        = "bat"
        /// ObjectUtils.toString(Boolean.TRUE) = "true"
        /// </pre>
        /// </summary>
        /// <seealso cref="StringUtils#defaultString(String)">StringUtils#defaultString(String)
        /// 	</seealso>
        /// <seealso cref="string.ToString(object)">string.ToString(object)</seealso>
        /// <param name="obj">the Object to <code>toString</code>, may be null</param>
        /// <returns>the passed in Object's toString, or nullStr if <code>null</code> input</returns>
        /// <since>2.0</since>
        public static string ToString(object obj)
        {
            return obj == null ? string.Empty : obj.ToString();
        }

        /// <summary>
        /// <p>Gets the <code>toString</code> of an <code>Object</code> returning
        /// a specified text if <code>null</code> input.</p>
        /// <pre>
        /// ObjectUtils.toString(null, null)           = null
        /// ObjectUtils.toString(null, "null")         = "null"
        /// ObjectUtils.toString("", "null")           = ""
        /// ObjectUtils.toString("bat", "null")        = "bat"
        /// ObjectUtils.toString(Boolean.TRUE, "null") = "true"
        /// </pre>
        /// </summary>
        /// <seealso cref="StringUtils#defaultString(String,String)">StringUtils#defaultString(String,String)
        /// 	</seealso>
        /// <seealso cref="string.ToString(object)">string.ToString(object)</seealso>
        /// <param name="obj">the Object to <code>toString</code>, may be null</param>
        /// <param name="nullStr">the String to return if <code>null</code> input, may be null
        /// 	</param>
        /// <returns>the passed in Object's toString, or nullStr if <code>null</code> input</returns>
        /// <since>2.0</since>
        public static string ToString(object obj, string nullStr)
        {
            return obj == null ? nullStr : obj.ToString();
        }

        // Min/Max
        //-----------------------------------------------------------------------
        /// <summary>Null safe comparison of Comparables.</summary>
        /// <remarks>Null safe comparison of Comparables.</remarks>
        /// <param name="c1">the first comparable, may be null</param>
        /// <param name="c2">the second comparable, may be null</param>
        /// <returns>
        /// <ul>
        /// <li>If both objects are non-null and unequal, the lesser object.
        /// <li>If both objects are non-null and equal, c1.
        /// <li>If one of the comparables is null, the non-null object.
        /// <li>If both the comparables are null, null is returned.
        /// </ul>
        /// </returns>
        public static object Min(IComparable c1, IComparable c2)
        {
            if (c1 != null && c2 != null)
            {
                return c1.CompareTo(c2) < 1 ? c1 : c2;
            }
            else
            {
                return c1 != null ? c1 : c2;
            }
        }

        /// <summary>Null safe comparison of Comparables.</summary>
        /// <remarks>Null safe comparison of Comparables.</remarks>
        /// <param name="c1">the first comparable, may be null</param>
        /// <param name="c2">the second comparable, may be null</param>
        /// <returns>
        /// <ul>
        /// <li>If both objects are non-null and unequal, the greater object.
        /// <li>If both objects are non-null and equal, c1.
        /// <li>If one of the comparables is null, the non-null object.
        /// <li>If both the comparables are null, null is returned.
        /// </ul>
        /// </returns>
        public static object Max(IComparable c1, IComparable c2)
        {
            if (c1 != null && c2 != null)
            {
                return c1.CompareTo(c2) >= 0 ? c1 : c2;
            }
            else
            {
                return c1 != null ? c1 : c2;
            }
        }

        /// <summary>
        /// <p>Class used as a null placeholder where <code>null</code>
        /// has another meaning.</p>
        /// <p>For example, in a <code>HashMap</code> the
        /// <see cref="System.Collections.Hashtable{K, V}.Get(object)">System.Collections.Hashtable&lt;K, V&gt;.Get(object)
        /// 	</see>
        /// method returns
        /// <code>null</code> if the <code>Map</code> contains
        /// <code>null</code> or if there is no matching key. The
        /// <code>Null</code> placeholder can be used to distinguish between
        /// these two cases.</p>
        /// <p>Another example is <code>Hashtable</code>, where <code>null</code>
        /// cannot be stored.</p>
        /// </summary>
        [System.Serializable]
        public class Null
        {
            /// <summary>Required for serialization support.</summary>
            /// <remarks>Required for serialization support. Declare serialization compatibility with Commons Lang 1.0
            /// 	</remarks>
            /// <seealso cref="System.IO.Serializable">System.IO.Serializable</seealso>
            private const long serialVersionUID = 7092611880189329093L;

            /// <summary>Restricted constructor - singleton.</summary>
            /// <remarks>Restricted constructor - singleton.</remarks>
            public Null() : base()
            {
            }

            // Null
            //-----------------------------------------------------------------------
            /// <summary><p>Ensure singleton.</p></summary>
            /// <returns>the singleton value</returns>
            private object ReadResolve()
            {
                return ObjectUtils.NULL;
            }
        }
    }
}
