
using System;
using System.Collections.Generic;

using Automation.Common;
using Automation.Runtime.Core;
using Automation.Runtime.Utils;

namespace Automation.Runtime
{

    public class EventBus : 
        SingletonMonoBehaviour<EventBus>
    {
        //---------------------------------------------------------------------
        private const string LogChannel = "eventbus";

        //---------------------------------------------------------------------
        public class SubscriptionToken
        {
            internal SubscriptionToken(string subject)
            {
                mUniqueTokenID = Guid.NewGuid();
                mSubject = subject;

                if (mSubject.Contains("*") || mSubject.Contains("?"))
                    mSubjectPattern = new WildcardPattern(mSubject);
                else
                    mSubjectPattern = null;
            }

            public Guid Token { get { return mUniqueTokenID; } }
            public string Subject { get { return mSubject; } }

            public bool SubjectMatch(string subject)
            {
                if (mSubjectPattern != null)
                {
                    return mSubjectPattern.IsMatch(subject);
                }
                else
                {
                    return mSubject == subject;
                }
            }

            private readonly Guid mUniqueTokenID;
            private readonly string mSubject;
            private readonly WildcardPattern mSubjectPattern;
        }

        //---------------------------------------------------------------------
        public class Subscription
        {
            public SubscriptionToken SubscriptionToken { get { return mSubscriptionToken; } }

            public Subscription(Action<string, object> action, SubscriptionToken token)
            {
                if (action == null)
                    throw new ArgumentNullException("action");

                if (token == null)
                    throw new ArgumentNullException("token");

                mAction = action;
                mSubscriptionToken = token;
            }

            public void Publish(string subject, object @event)
            {
                if (@event == null)
                    throw new ArgumentNullException("@event");

                mAction.Invoke(subject, @event);
            }

            protected readonly Action<string, object> mAction;
            protected readonly SubscriptionToken mSubscriptionToken;
        }
        
        public class Subscription<TEvent> : Subscription
        {
            private static Action<string, object> WrapActionFunc(Action<string, TEvent> action) => (subject, @event) => action(subject, (TEvent)@event);
            
            public Subscription(Action<string, TEvent> action, SubscriptionToken token) :
                base(WrapActionFunc(action), token)
            { }
            
            public void Publish(string subject, TEvent @event)
            {
                if (@event == null)
                    throw new ArgumentNullException("@event");

                mAction.Invoke(subject, @event);
            }
        }

        //---------------------------------------------------------------------
        private readonly List<Subscription> mSubscriptions;
        private readonly object mSubscriptionsLock = new object();

        //---------------------------------------------------------------------
        public EventBus()
        {
            mSubscriptions = new List<Subscription>(16);
        }
        
        //---------------------------------------------------------------------
        /// <summary>
        /// Subscribes to the specified event type with the specified action
        /// </summary>
        /// <param name="action">The Action to invoke when an event of this type is published</param>
        /// <returns>A <see cref="SubscriptionToken"/> to be used when calling <see cref="Unsubscribe"/></returns>
        public SubscriptionToken Subscribe(string subjectPattern, Action<string, object> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (mSubscriptionsLock)
            {
                SubscriptionToken token = new SubscriptionToken(subjectPattern);
                mSubscriptions.Add(new Subscription(action, token));
                
                return token;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Subscribes to the specified event type with the specified action
        /// </summary>
        /// <param name="ation">The Action to invoke when an event of this type is published</param>
        /// <returns>A <see cref="SubscriptionToken"/> to be used when calling <see cref="Unsubscribe"/></returns>
        public SubscriptionToken Subscribe<TEvent>(string subjectPattern, Action<string, TEvent> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            lock (mSubscriptionsLock)
            {
                SubscriptionToken token = new SubscriptionToken(subjectPattern);
                mSubscriptions.Add(new Subscription<TEvent>(action, token));
                
                return token;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Unsubscribe from the Event type related to the specified <see cref="SubscriptionToken"/>
        /// </summary>
        /// <param name="token">The <see cref="SubscriptionToken"/> received from calling the Subscribe method</param>
        public void Unsubscribe(SubscriptionToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            lock (mSubscriptionsLock)
            {
                int idx = mSubscriptions.FindIndex((s) => s.SubscriptionToken == token);
                if (idx >= 0)
                {
                    mSubscriptions.RemoveAt(idx);
                }
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Publishes the specified event to any subscribers for the <see cref="TEvent"/> event type
        /// </summary>
        /// <typeparam name="TEvent">The type of event</typeparam>
        /// <param name="event">Event to publish</param>
        public void Publish<TEvent>(string subject, TEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException("@event");

            List<Subscription> subscriptions = new List<Subscription>();
            lock (mSubscriptionsLock)
            {
                for (int idx = 0; idx < mSubscriptions.Count; ++idx)
                {
                    Subscription subscription = mSubscriptions[idx];
                    if (subscription.SubscriptionToken.SubjectMatch(subject))
                        subscriptions.Add(subscription);
                }
            }

            for (int idx = 0; idx < subscriptions.Count; ++idx)
            {
                try
                {
                    subscriptions[idx].Publish(subject, @event);
                }
                catch (Exception exception)
                {
                    Logger.Error(LogChannel, exception.Message);
                }
            }
        }

    }
}
