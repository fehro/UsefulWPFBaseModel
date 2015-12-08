using System;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/// <summary>
/// ---------------------------------------------------------------------------------------
/// Useful WPF Base Model
/// ---------------------------------------------------------------------------------------
/// A useful WPF base model class that allows:
/// - Change tracking.
/// - Data validation.
/// ---------------------------------------------------------------------------------------
/// Requires GalaSoft MVVM Light.
/// http://www.mvvmlight.net/
/// ---------------------------------------------------------------------------------------
/// </summary>
public abstract class UsefulWpfBaseModel : ViewModelBase, IChangeTracking
{
    #region Global Variables / Properties

    protected const string EmailRegex = "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$";

    protected List<KeyValuePair<string, object>> _pristineValues;

    protected bool _changeTracking;

    /// <summary>
    /// The model's state. This closely mimics the ASP.Net MVC model state for familiarity.
    /// </summary>
    public List<KeyValuePair<string, List<string>>> ModelState
    {
        get
        {
            var returnValue = new List<KeyValuePair<string, List<string>>>();

            //Get all the properties with the validation attribute.
            var properties = GetPropertiesWithAttribute(typeof(ValidationAttribute));

            //Perform email validation on the provided properties.
            var errors = PerformEmailValidation(GetPropertiesWithAttribute(properties, typeof(EmailValidationAttribute)));
            AddModelStateErrors(ref returnValue, errors);

            //Perform required validation on the provided properties.
            errors = PerformRequiredValidation(GetPropertiesWithAttribute(properties, typeof(RequiredValidationAttribute)));
            AddModelStateErrors(ref returnValue, errors);

            return returnValue;
        }
    }

    /// <summary>
    /// Return true or false if the model is valid.
    /// </summary>
    public bool IsValid
    {
        get
        {
            return !ModelState.Any();
        }
    }

    #endregion

    #region Constructor

    protected UsefulWpfBaseModel()
    {
        _changeTracking = false;
        PropertyChanged += new PropertyChangedEventHandler(OnNotifiedOfPropertyChanged);
        _pristineValues = new List<KeyValuePair<string, object>>();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Get the pristine value (if any) for the provided property expression.
    /// </summary>
    protected object GetPristineValue<T>(Expression<Func<T>> propertyExpression)
    {
        return GetPristineValue(GetPropertyName(propertyExpression));
    }

    /// <summary>
    /// Get the pristine value (if any) for the provided property name.
    /// </summary>
    protected object GetPristineValue(string propertyName)
    {
        return _pristineValues.SingleOrDefault(x => x.Key == propertyName).Value;
    }

    /// <summary>
    /// Get the property value for the provided property expression.
    /// </summary>
    protected object GetPropertyValue<T>(Expression<Func<T>> propertyExpression)
    {
        return GetPropertyValue(GetPropertyName(propertyExpression));
    }

    /// <summary>
    /// Get the property value for the provided property name.
    /// </summary>
    protected object GetPropertyValue(string propertyName)
    {
        var propertyInfo = GetType().GetProperty(propertyName);

        return propertyInfo.GetValue(this);
    }

    /// <summary>
    /// Return true or false if the property with the provided name is trackable.
    /// </summary>
    protected bool IsTrackableProperty(string propertyName)
    {
        var propertyInfo = GetType().GetProperty(propertyName);

        return Attribute.IsDefined(propertyInfo, typeof(TrackableAttribute));
    }

    /// <summary>
    /// Set the pristine value for the property with the provided name.
    /// </summary>
    protected void SetPristineValue(string propertyName, object propertyValue)
    {
        //See if the pristine value has been set yet.
        var index = _pristineValues.FindIndex(x => x.Key == propertyName);

        if (index == -1)
        {
            //Property value has not been added yet to the list of pristine values. So add it.
            _pristineValues.Add(new KeyValuePair<string, object>(propertyName, propertyValue));
            return;
        }

        //Property value has already been added so update it.
        _pristineValues[index] = new KeyValuePair<string, object>(propertyName, propertyValue);
    }

    /// <summary>
    /// If we are tracking changes and a trackable property has changed 
    /// then raise a property changed event for the IsChanged property. 
    /// </summary>
    protected void OnNotifiedOfPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsValid") return;

        if (_changeTracking && e != null && IsTrackableProperty(e.PropertyName))
            RaisePropertyChanged(() => IsChanged);

        RaisePropertyChanged(() => IsValid);
    }

    /// <summary>
    /// Return true or false if the values are equal.
    /// </summary>
    protected bool AreEqual(object value1, object value2)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(value1) ==
            Newtonsoft.Json.JsonConvert.SerializeObject(value2);
    }

    /// <summary>
    /// Get the properties in the model that have the provided attribute type.
    /// </summary>
    protected List<System.Reflection.PropertyInfo> GetPropertiesWithAttribute(Type type)
    {
        return GetPropertiesWithAttribute(GetType().GetProperties().ToList(), type);
    }

    /// <summary>
    /// Get the properties in the provided list that have the provided attribute type.
    /// </summary>
    protected List<System.Reflection.PropertyInfo> GetPropertiesWithAttribute(List<System.Reflection.PropertyInfo> properties, Type type)
    {
        return properties.Where(prop => Attribute.IsDefined(prop, type)).ToList();
    }

    /// <summary>
    /// Populate the pristine values for each trackable property.
    /// </summary>
    protected void PopulatePristineValues()
    {
        //Get all the properties with the trackable attribute.
        var properties = GetPropertiesWithAttribute(typeof(TrackableAttribute));

        //Loop the properties saving their values to the list of pristine values.
        foreach (var property in properties)
        {
            //Get the property value.
            var propertyValue = GetPropertyValue(property.Name);

            //Set the pristine value.
            SetPristineValue(property.Name, propertyValue);
        }
    }

    /// <summary>
    /// Perform required validation for the provided list of properties.
    /// </summary>
    protected List<KeyValuePair<string, List<string>>> PerformRequiredValidation(List<System.Reflection.PropertyInfo> properties)
    {
        var returnValue = new List<KeyValuePair<string, List<string>>>();

        //Loop the properties validating them.
        foreach (var property in properties)
        {
            //Get the property value.
            var propertyValue = GetPropertyValue(property.Name);

            if (propertyValue == null || string.IsNullOrEmpty(propertyValue.ToString()))
                //The property value is null or empty. Add an error to the model state.
                AddModelStateError(ref returnValue, property.Name, string.Format("{0} is required", property.Name));
        }

        return returnValue;
    }

    /// <summary>
    /// Perform email validation for the provided list of properties.
    /// </summary>
    protected List<KeyValuePair<string, List<string>>> PerformEmailValidation(List<System.Reflection.PropertyInfo> properties)
    {
        var returnValue = new List<KeyValuePair<string, List<string>>>();

        var regex = new Regex(EmailRegex);

        //Loop the properties validating them.
        foreach (var property in properties)
        {
            //Get the property value.
            var propertyValue = GetPropertyValue(property.Name);

            //Skip the value if it is null.
            if (propertyValue == null) continue;

            //If the type of the property value is not string throw an exception.
            if (!(propertyValue is string))
                throw new Exception(string.Format("Cannot perform email validation on property {0} which is not of type string", property.Name));

            if (!regex.IsMatch(propertyValue.ToString()))
                //The property value is not a valid email. Add an error to the model state.
                AddModelStateError(ref returnValue, property.Name, string.Format("{0} is not a valid email", property.Name));

        }

        return returnValue;
    }

    /// <summary>
    /// Add the provided model state errors to the list of existing model state errors.
    /// </summary>
    protected void AddModelStateErrors(ref List<KeyValuePair<string, List<string>>> existingErrors, List<KeyValuePair<string, List<string>>> errors)
    {
        foreach (var error in errors)
        {
            AddModelStateErrors(ref existingErrors, error.Key, error.Value);
        }
    }

    /// <summary>
    /// Add the provided errors to the list of existing model state errors.
    /// </summary>
    protected void AddModelStateErrors(ref List<KeyValuePair<string, List<string>>> existingErrors, string propertyName, List<string> errors)
    {
        foreach (var error in errors)
        {
            AddModelStateError(ref existingErrors, propertyName, error);
        }
    }

    /// <summary>
    /// Add the provided property name and error message to the existing list of model state errors.
    /// </summary>
    protected void AddModelStateError(ref List<KeyValuePair<string, List<string>>> existingErrors, string propertyName, string errorMessage)
    {
        var index = existingErrors.FindIndex(x => x.Key == propertyName);

        if (index == -1)
        {
            //The property has no errors yet.
            existingErrors.Add(new KeyValuePair<string, List<string>>(propertyName, new List<string> { errorMessage }));
            return;
        }

        //The property already has at least one error.
        existingErrors[index].Value.Add(errorMessage);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Return true or false if the provided property has changed from it's pristine value.
    /// </summary>
    public bool HasChanged<T>(Expression<Func<T>> propertyExpression)
    {
        //If change tracking is not enabled throw an exception.
        if (!_changeTracking) throw new Exception("Change tracking is not enabled");

        //Get the pristine value (if any).
        var pristineValue = GetPristineValue(propertyExpression);

        //Get the property value.
        var propertyValue = GetPropertyValue(propertyExpression);

        return !AreEqual(pristineValue, propertyValue);
    }

    /// <summary>
    /// Enable change tracking.
    /// </summary>
    public void EnableChangeTracking()
    {
        //If change tracking is already enabled then throw an exception.
        if (_changeTracking) throw new Exception("Change tracking is already enabled");

        _changeTracking = true;

        //Populate the pristine values.
        PopulatePristineValues();
    }

    #endregion

    #region Implemented IChangeTracking Members

    /// <summary>
    /// Track if the model has changed.
    /// </summary>
    public bool IsChanged
    {
        get
        {
            //Get all the properties with the trackable attribute.
            var properties = GetType().GetProperties().Where(prop =>
                Attribute.IsDefined(prop, typeof(TrackableAttribute))).ToList();

            //Loop till a change is found.
            foreach (var property in properties)
            {
                //Get the pristine value (if any).
                var pristineValue = GetPristineValue(property.Name);

                //Get the property value.
                var propertyValue = GetPropertyValue(property.Name);

                //If the property has changed return true.
                if (!AreEqual(propertyValue, pristineValue))
                    return true;
            }

            //No changes found. Return false.
            return false;
        }
    }

    /// <summary>
    /// Resets the model's state to unchanged by accepting the modifications.
    /// </summary>
    public void AcceptChanges()
    {
        //If change tracking is not enabled throw an exception.
        if (!_changeTracking) throw new Exception("Change tracking is not enabled");

        //Populate the pristine values.
        PopulatePristineValues();

        //Raise a property changed event for the IsChanged property.
        RaisePropertyChanged(() => IsChanged);
    }

    #endregion
}

[AttributeUsage(AttributeTargets.Property)]
public class TrackableAttribute : System.Attribute { }

public class ValidationAttribute : System.Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class EmailValidationAttribute : ValidationAttribute { }

[AttributeUsage(AttributeTargets.Property)]
public class RequiredValidationAttribute : ValidationAttribute { }