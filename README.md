# JsonModelBinder

[Nuget](https://www.nuget.org/packages/MoonLight.JsonModelBinder/)

JsonModelBinder allows you to add patching capabilities to your Models.

## How it works

When you send data to the server you send either a Model or IEnumerable\<Model>; the library works with both in the same way.

Consider the below two JSONs:

```javascript
{
    // 'id': 1, // id is optional
    'name': 'First Post', // Update the post name
    //'content': 'Post Content', // no need to update the content
    'attachments': [
        {
            '_patchType': 1, // add a new attachment
            'name': 'Section 1.1',
            'data': 'base64_data'
        },
        {
            '_patchType': 0, // delete attachment with id 2
            'id': 2,
        },
        {
            '_patchType': 2, // update the name of an existing attachment with id 3
            'id': 3,
            'name': 'Section 1.2'
        }
    ]
}
```
To use JsonModelBinder you need to serialize the model as normal JSON. When patching a single document, it's optional to include the model PKs as it won't affect the patching of the model (see usage below).

Note that when patching an array, an extra property (`_patchType`) needs to be included which will identify if the array entry will be created, patched or deleted. If applying the create operation on the root document or array, this property is ignored as all the entries will be created.

```javascript
[
    {
        '_patchType': 1,
        'name': 'First Post',
        'content': 'Post Content',
        'attachments': [
            {
                'name': 'file1.png',
                'data': 'base64_data'
            },
            {
                'name': 'file2.png',
                'data': 'base64_data'
            },
        ]
    },
    {
        '_patchType': 2,
        'id': 4,
        'name': 'Second Post',
        'content': 'Post Content',
        'attachments': [
            {
                '_patchType': 1,
                'name': 'file1.png',
                'data': 'base64_data'
            },
            {
                '_patchType': 1,
                'name': 'file2.png',
                'data': 'base64_data'
            },
        ]
    },
    {
        '_patchType': 0,
        'id': 2
    }
]
```

When patching an array of models, the json is similar to when patching a single model except now you'll need to send them as array.

## Usage

In the Controller, the actions parameters needs to be one of these types: `IPatchArray<T>`, `PatchArray<T>`, `IPatchDocument<T>` or `PatchDocument<T>`.

Use `IPatchArray<T>` or `PatchArray<T>` if you want to patch multiple models at once.

Use `IPatchDocument<T>` or `PatchDocument<T>` if you want to patch a single model.


```csharp 
//ControllerAction

public async Task<JsonResult> Patch([FromBody] IPatchDocument<T> post)
{
}

public async Task<JsonResult> Patch([FromBody] IPatchArray<T> posts)
{
}
```

Models:

```csharp
class Post
{
    [Key]
    public int Id { get; set; }
    [MaxLength(20)]
    [Required]
    public string Name { get; set; }
    
    public ICollection<Attachment> Attachments { get; set; }

    public string Content { get; set; }

    [IgnorePatch]
    public int AuthorId { get; set; }
}

class Attachment
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    public byte[] Data { get; set; }

    public int PostId { get; set; }

    [IgnorePatch]
    public virtual Post Post { get; set; }
}

```
A single Model is called `PatchDocument`.

An array of Models is called `PatchArray`.

If a model is part of an array, it is called `PatchArrayDocument`. A `PatchArrayDocument` is the same as a normal `PatchDocument` except with two differences:
1. Every `PatchArrayDocument` needs to have an property (`_patchType`) sent with the json (ignore if creating).
2. The PKs needs to be decorated with the [Key](https://msdn.microsoft.com/en-us/library/system.componentmodel.dataannotations.keyattribute%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396) attribute.

Once ready, you'll can invoke one of the two APIs to apply the patching:
1. Apply: Use if you want to update existing model(s).
2. ApplyNew: Use if you want to create new model(s).

```csharp
public async Task<JsonResult> Patch([FromBody] IPatchDocument<Post> post)
{
    // Add new post
    var newPost = new Post();

    await post.ApplyNew(newPost);

    // Patch existing post
    var existingPost = postRepository.Find(4);

    await post.Apply(existingPost);
}

public async Task<JsonResult> Patch([FromBody] IPatchArray<T> posts)
{
    // Add new posts
    var newPost = new List<Post>();

    await posts.ApplyNew(newPosts);

    // Patch existing posts
    var existingPosts = postRepository.ToList();

    await posts.Apply(existingPosts);
}
```

## Checking for errors

The library checks for the [Required](https://msdn.microsoft.com/en-us/library/system.componentmodel.dataannotations.requiredattribute(v=vs.110).aspx) and validation attributes when construting the patch model (array or a single). If errors were found they will be marked with an error kind:
1. ApplyToCreate: The error will prevent the creation of a new model (ex: The `name` property is marked as Required and missing from the json payload).
2. ApplyToUpdate: The error will prevent the patching of an existing model (ex: `_patchType` is missing from one of the `PathArrayDocument`s).
3. ApplyToAll: The error will prevent applying any of the operations on the model (ex: the `name` property violates the MaxLength(20) validation attribute).

Every IPatchArray, IPatchDocument, IPatchArrayDocument or IPatchPrimitive will have APIs to help identify the patching issues:
1. `bool CanCreate()`: Checks if there are errors related to Model creation.
2. `bool CanPatch()`: Checks if there are errors related to Model patching.
3. `bool HasErrors()`: Checks if the PatchModel has any kind of error.
4. `bool HasErrors(ErrorKinds errorKind)`: Checks if the PatchModel has errors of the specified kind.
5. `IEnumerable<Error> Errors`: Returns all the errors found in the PatchModel.

## Supporting BSON formats

You can add the support to the BSON format with a couple of lines:

Create the formatter:
```csharp
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

public class BsonInputFormatter : IInputFormatter
{
    public bool CanRead(InputFormatterContext context)
    {
        return context.HttpContext.Request.ContentType == "application/bson";
    }

    public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
    {
        return Task.Factory.StartNew(() =>
        {
            var converter = context.ModelType.GetTypeInfo().GetCustomAttribute<JsonConverterAttribute>();

            if (converter == null) return InputFormatterResult.Failure();

            var concreteConverter =
                (JsonConverter)Activator.CreateInstance(converter.ConverterType, converter.ConverterParameters);
            
            var reader = new BsonDataReader(context.HttpContext.Request.Body);
            
            var obj = concreteConverter.ReadJson(reader, context.ModelType, null, JsonSerializer.CreateDefault());

            return InputFormatterResult.Success(obj);
        });
    }
}
```

Add support in the `startup.cs` file:

```csharp
services.AddMvc(x =>
{
    x.InputFormatters.Add(new BsonInputFormatter());
});
```
