# Useful WPF Base Model

A base class for your WPF models that supports:

  - Change tracking.
  - Validation.
 
This class requires the MVVM Light Toolkit from GalaSoft available at
http://www.mvvmlight.net

### Usage
Inherit from the UsefulWpfBaseModel class in your model.
```sh
public class User : UsefulWpfBaseModel
{
}
```
### Change Tracking
Decorate any properties you want to track with the [Trackable] attribute. Ensure the property setter calls the Set() method in the GalaSoft ViewModelBase so the property change event is raised.
```sh
private string _name;
[Trackable]
public string Name {
    get { return _name; }
    set { Set(ref _name, value); }
}
```
To start tracking changes to your model call the EnableChangeTracking() method on your model after you have finished populating it initially.
```sh
var user = new User();
user.Name = 'Bill';
user.EnableChangeTracking();
```
The IsChanged property in your model can be observed from XAML if for example you want to tell the user that there are unsaved changes.
```sh
<TextBlock Visibility="{Binding CurrentUser.IsChanged, Converter={StaticResource BooleanToVisibilityConverter}}">Unsaved Changes</TextBlock>
```
If you want to check if a property has changed from your code this can be done with the HasChanged() method on your model.
```sh
if (CurrentUser.HasChanged(() => CurrentUser.Name))
{
    //The Name property has changed. Do something.
}
```
Once you have saved your model you want to call the AcceptChanges() method on the model so you can track any further changes made to what values have just been saved.
```sh
if (UserProvider.Save(ref user))
{
    //User has been saved.
    user.AcceptChanges();
}
```
### Email Validation
Decorate any properties you want to validate in your model for a valid email address with the [EmailValidation] attribute. If you want to bind your XAML to the IsValid property then ensure the property setter calls the Set() method in the GalaSoft ViewModelBase so the property change event is raised.
```sh
private string _email;
[EmailValidation]
public string Email {
    get { return _email; }
    set { Set(ref _email, value); }
}
```

To validate your model call the IsValid property on your model or the ModelState property to get the list of errors.
```sh
if(CurrentUser.IsValid)
{
    //Do something with the valid model.
    
    return;
}

//Do something with the errors.
var errors = CurrentUser.ModelState;
```
### Required Validation
Decorate any properties you want to validate in your model as required with the [RequiredValidation] attribute. If you want to bind your XAML to the IsValid property then ensure the property setter calls the Set() method in the GalaSoft ViewModelBase so the property change event is raised.
```sh
private string _name;
[RequiredValidation]
public string Name {
    get { return _name; }
    set { Set(ref _name, value); }
}
```