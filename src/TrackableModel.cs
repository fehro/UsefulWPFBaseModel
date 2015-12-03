using System;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

public abstract class TrackableModel : ViewModelBase, IChangeTracking
{
    #region Global Variables / Properties

    //The list of pristine values.
    private List<KeyValuePair<string, object>> _pristineValues;

    private bool _changeTracking;

    #endregion

    #region Constructor

    protected TrackableModel()
    {
        _changeTracking = false;
        PropertyChanged += new PropertyChangedEventHandler(OnNotifiedOfPropertyChanged);
        _pristineValues = new List<KeyValuePair<string, object>>();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Get the pristine value (if any) for the provided property expression.
    /// </summary>
    private object GetPristineValue<T>(Expression<Func<T>> propertyExpression)
    {
        return GetPristineValue(GetPropertyName(propertyExpression));
    }

    /// <summary>
    /// Get the pristine value (if any) for the provided property name.
    /// </summary>
    private object GetPristineValue(string propertyName)
    {
        return _pristineValues.SingleOrDefault(x => x.Key == propertyName).Value;
    }

    /// <summary>
    /// Get the property value for the provided property expression.
    /// </summary>
    private object GetPropertyValue<T>(Expression<Func<T>> propertyExpression)
    {
        return GetPropertyValue(GetPropertyName(propertyExpression));
    }

    /// <summary>
    /// Get the property value for the provided property name.
    /// </summary>
    private object GetPropertyValue(string propertyName)
    {
        var propertyInfo = GetType().GetProperty(propertyName);

        return propertyInfo.GetValue(this);
    }

    /// <summary>
    /// Return true or false if the property with the provided name is trackable.
    /// </summary>
    private bool IsTrackableProperty(string propertyName)
    {
        var propertyInfo = GetType().GetProperty(propertyName);

        return Attribute.IsDefined(propertyInfo, typeof(TrackableAttribute));
    }

    /// <summary>
    /// Set the pristine value for the property with the provided name.
    /// </summary>
    private void SetPristineValue(string propertyName, object propertyValue)
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
    private void OnNotifiedOfPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (_changeTracking && e != null && IsTrackableProperty(e.PropertyName))
            RaisePropertyChanged(() => IsChanged);
    }

    /// <summary>
    /// Return true or false if the values are equal.
    /// </summary>
    private bool AreEqual(object value1, object value2)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(value1) ==
            Newtonsoft.Json.JsonConvert.SerializeObject(value2);
    }

    /// <summary>
    /// Populate the pristine values for each trackable property.
    /// </summary>
    private void PopulatePristineValues()
    {
        //Get all the properties with the trackable attribute.
        var properties = GetType().GetProperties().Where(prop =>
            Attribute.IsDefined(prop, typeof(TrackableAttribute))).ToList();

        //Loop the properties saving their values to the list of pristine values.
        foreach (var property in properties)
        {
            //Get the property value.
            var propertyValue = GetPropertyValue(property.Name);

            //Set the pristine value.
            SetPristineValue(property.Name, propertyValue);
        }
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
}

[AttributeUsage(AttributeTargets.Property)]
public class TrackableAttribute : System.Attribute { }