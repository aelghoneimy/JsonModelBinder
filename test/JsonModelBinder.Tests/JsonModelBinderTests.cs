namespace JsonModelBinder.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Converters;
    using Interfaces;
    using Models;

    [TestClass]
    public class JsonModelBinderTests
    {
        private PatchDocumentJsonConverter _converter;
        private TextReader _textReader;
        private JsonReader _jsonReader;

        [TestInitialize]
        public void Initialize()
        {
            _converter = new PatchDocumentJsonConverter();
        }
        
        [TestMethod]
        public void PatchDocumentConverterReturnsConcreteClass()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 50, "
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z', "
                    + $"'{nameof(Blog.Image)}': [102, 200, 32, 45, 255], "
                    //+ "{"
                    //    + $"'{nameof(TestObject.Name)}': 'KKK',"
                    //    + $"'{nameof(TestObject.DateTime)}': '2016-06-16T23:25:36Z',"
                    //    + $"'{nameof(TestObject.Number)}': 102,"
                    //+ "}, " 
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{"
                            //+ $"'{nameof(Post.PostId)}': 10,"
                            + $"'{nameof(Post.Title)}': 'My Awesome post',"
                            + $"'{nameof(Post.Visibility)}': 1,"
                            + "'_patchType': 1"
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var concreteObject = _converter.ReadJson(_jsonReader, typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            Assert.IsNotNull(concreteObject);
            Assert.IsTrue(concreteObject is PatchDocument<Blog>);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void PatchDocumentConverterReturnsErrorWhenInvalidJson()
        {
            // Assign
            _textReader = new StringReader(
                "{[]}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var patchDocument = _converter.ReadJson(_jsonReader, typeof(PatchDocument<Blog>), null, new JsonSerializer())
                as PatchDocument<Blog>;

            // Assert
            Assert.IsNotNull(patchDocument);
            Assert.IsFalse(patchDocument.HasValue);
            Assert.IsTrue(patchDocument.HasErrors());
            Assert.IsTrue(patchDocument.Errors.First().ErrorType == typeof(JsonReaderException));
        }

        [TestMethod]
        public void PatchDocumentConverterAddsIntValue()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 50, "
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            const string expectedKey = nameof(Blog.NumberofSubscribers);
            var expectedKeyType = typeof(short);
            const short expectedKeyValue = 20;

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, JsonSerializer.CreateDefault());

            // Assert
            Assert.IsTrue(patchDocument.Contains(expectedKey), "Key not found");

            Assert.AreEqual(PatchKinds.Primitive, patchDocument[expectedKey].Kind, "Wrong kind");

            var value = ((IPatchPrimitive)patchDocument[expectedKey]).Value;

            Assert.AreEqual(expectedKeyType, value.GetType(), "Wrong value type");
            Assert.AreEqual(expectedKeyValue, value, "Wrong value");
        }
        
        [TestMethod]
        public void PatchDocumentConverterAddsDateTimeValue()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 50, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z'"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            const string expectedKey = nameof(Blog.CreatedOn);
            var expectedKeyType = typeof(DateTime);
            var expectedKeyValue = new DateTime(2016, 06, 16, 23, 25, 36);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            Assert.IsTrue(patchDocument.Contains(expectedKey), "Key not found");

            Assert.AreEqual(PatchKinds.Primitive, patchDocument[expectedKey].Kind, "Wrong kind");

            var value = ((IPatchPrimitive)patchDocument[expectedKey]).Value;

            Assert.AreEqual(expectedKeyType, value.GetType(), "Wrong value type");
            Assert.AreEqual(expectedKeyValue, value, "Wrong value");
        }

        [TestMethod]
        public void PatchDocumentConverterAddsEnumValue()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 50, "
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z', "
                    + $"'{nameof(Blog.Image)}': [102, 200, 32, 45, 255], "
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{"
                            //+ $"'{nameof(Post.PostId)}': 10,"
                            + $"'{nameof(Post.Title)}': 'My Awesome post',"
                            + $"'{nameof(Post.Visibility)}': 1,"
                            + "'_patchType': 1"
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            const string expectedKey = nameof(Post.Visibility);
            var expectedKeyType = typeof(Visibility);
            const Visibility expectedKeyValue = Visibility.Public;

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            var patchArray = (PatchArray<Post>)patchDocument[nameof(Blog.Posts)];
            var post = patchArray.Values.FirstOrDefault();
            Assert.IsNotNull(post, "Post not mapped");
            Assert.IsTrue(post.Contains(expectedKey), "Key not found");

            Assert.AreEqual(PatchKinds.Primitive, post[expectedKey].Kind, "Wrong kind");

            var value = ((IPatchPrimitive)post[expectedKey]).Value;

            Assert.AreEqual(expectedKeyType, value.GetType(), "Wrong value type");
            Assert.AreEqual(expectedKeyValue, value, "Wrong value");
        }

        [TestMethod]
        public void PatchDocumentConverterErrorExceedsRange()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 50, "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 50"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            const string expectedKey = nameof(Blog.NumberofSubscribers);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            var firstValue = patchDocument[expectedKey];

            Assert.IsTrue(firstValue.HasErrors(), "Operation Value should have errors");
        }

        [TestMethod]
        public void PatchDocumentShouldCreate()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z', "
                    + $"'{nameof(Blog.Image)}': [102, 200, 32, 45, 255], "
                    + $"'{nameof(Blog.TestObject)}': {{"
                        + $"'{nameof(TestObject.Name)}': 'KKK', "
                        + $"'{nameof(TestObject.DateTime)}': '2016-06-16T23:25:36Z', "
                        + $"'{nameof(TestObject.Number)}': 102, "
                    + "}, "
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{"
                            + $"'{nameof(Post.BlogId)}': 1, "
                            + $"'{nameof(Post.Title)}': 'My Awesome post', "
                            + $"'{nameof(Post.Visibility)}': 1, "
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);
            
            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            Assert.IsTrue(patchDocument.CanCreate(), "PatchDocument should create");
            Assert.IsFalse(patchDocument.HasErrors(ErrorKinds.ApplyToCreate), "PatchDocument shouldn't have errors");
        }

        [TestMethod]
        public void PatchDocumentShouldNotCreate()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z', "
                    + $"'{nameof(Blog.Image)}': [102, 200, 32, 45, 255], "
                    + $"'{nameof(Blog.TestObject)}': {{"
                        + $"'{nameof(TestObject.Name)}': 'KKK', "
                        + $"'{nameof(TestObject.DateTime)}': '2016-06-16T23:25:36Z', "
                        + $"'{nameof(TestObject.Number)}': 102, "
                    + "}, "
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{"
                            + $"'{nameof(Post.Title)}': 'My Awesome post', "
                            + $"'{nameof(Post.Visibility)}': 1, "
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            Assert.IsFalse(patchDocument.CanCreate(), "PatchDocument should create");
            Assert.IsTrue(patchDocument.HasErrors(ErrorKinds.ApplyToCreate), "PatchDocument shouldn't have errors");
        }

        [TestMethod]
        public void PatchDocumentShouldUpdate()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z', "
                    + $"'{nameof(Blog.Image)}': [102, 200, 32, 45, 255], "
                    + $"'{nameof(Blog.TestObject)}': {{"
                        + $"'{nameof(TestObject.Name)}': 'KKK', "
                        + $"'{nameof(TestObject.DateTime)}': '2016-06-16T23:25:36Z', "
                        + $"'{nameof(TestObject.Number)}': 102, "
                    + "}, "
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{"
                            + $"'{nameof(Post.BlogId)}': 1, "
                            + $"'{nameof(Post.Title)}': 'My Awesome post', "
                            + $"'{nameof(Post.Visibility)}': 1, "
                            + "_patchType: 1"
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            Assert.IsTrue(patchDocument.CanPatch(), "PatchDocument should patch");
            Assert.IsFalse(patchDocument.HasErrors(ErrorKinds.ApplyToUpdate), "PatchDocument shouldn't have errors");
        }

        [TestMethod]
        public void PatchDocumentShouldNotUpdate()
        {
            // Assign
            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.Name)}': 'My Awesome Blog', "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20, "
                    + $"'{nameof(Blog.CreatedOn)}': '2016-06-16T23:25:36Z', "
                    + $"'{nameof(Blog.Image)}': [102, 200, 32, 45, 255], "
                    + $"'{nameof(Blog.TestObject)}': {{"
                        + $"'{nameof(TestObject.Name)}': 'KKK', "
                        + $"'{nameof(TestObject.DateTime)}': '2016-06-16T23:25:36Z', "
                        + $"'{nameof(TestObject.Number)}': 102, "
                    + "}, "
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{"
                            + $"'{nameof(Post.BlogId)}': 1, "
                            + $"'{nameof(Post.Title)}': 'My Awesome post', "
                            + $"'{nameof(Post.Visibility)}': 1, "
                            + "_patchType: 2"
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader,
                typeof(PatchDocument<Blog>), null, new JsonSerializer());

            // Assert
            Assert.IsFalse(patchDocument.CanPatch(), "PatchDocument should patch");
            Assert.IsTrue(patchDocument.HasErrors(ErrorKinds.ApplyToUpdate), "PatchDocument shouldn't have errors");
        }

        [TestMethod]
        public async Task RemoveFromList()
        {
            // Assign
            var blog = new Blog { BlogId = 10, CreatedOn = new DateTime(), Name = "Blog 1", NumberofSubscribers = 15 };
            var posts = new List<Post>
            {
                new Post { PostId = 10, BlogId = 10, Blog = blog, Title = "Title 10" },
                new Post { PostId = 11, BlogId = 10, Blog = blog, Title = "Title 11" }
            };

            blog.Posts.Add(posts[0]);
            blog.Posts.Add(posts[1]);

            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 10, "
                    + $"'{nameof(Blog.Posts)}': ["
                        + "{ "
                        + $"'{nameof(Post.PostId)}': 10, "
                        + $"'{nameof(Post.BlogId)}': 10, "
                        + "'_patchType': 0"
                        + "},"

                        + "{ "
                        + $"'{nameof(Post.PostId)}': 11, "
                        + $"'{nameof(Post.BlogId)}': 10, "
                        + "'_patchType': 0"
                        + "}"
                    + "]"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader, typeof(PatchDocument<Blog>), null, new JsonSerializer());

            await patchDocument.Apply(blog);

            // Assert
            Assert.IsNull(blog.Posts.FirstOrDefault(x => x.PostId == 10));
            Assert.IsNull(blog.Posts.FirstOrDefault(x => x.PostId == 11));
        }

        [TestMethod]
        public async Task DefaultValueUpdate()
        {
            // Assign
            var blog = new Blog { BlogId = 10, CreatedOn = new DateTime(), Name = "Blog 1", NumberofSubscribers = 15 };
            var posts = new List<Post>
            {
                new Post { PostId = 10, BlogId = 10, Blog = blog, Title = "Title 10" },
                new Post { PostId = 11, BlogId = 10, Blog = blog, Title = "Title 11" }
            };

            blog.Posts.Add(posts[0]);
            blog.Posts.Add(posts[1]);

            _textReader = new StringReader(
                "{"
                    + $"'{nameof(Blog.BlogId)}': 10, "
                    + $"'{nameof(Blog.NumberofSubscribers)}': 20"
                + "}");

            _jsonReader = new JsonTextReader(_textReader);

            // Arrange
            var patchDocument = (PatchDocument<Blog>)_converter.ReadJson(_jsonReader, typeof(PatchDocument<Blog>), null, new JsonSerializer());

            await patchDocument.Apply(blog);

            // Assert
            Assert.AreEqual(20, blog.NumberofSubscribers);
        }
    }
}