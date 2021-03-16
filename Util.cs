using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using UltraDES;

namespace UltraDESWeb
{
    public static class Util
    { 
        public static async void Alert(this IJSRuntime js, string msg) =>
            await js.InvokeVoidAsync("alert", msg);

        public static async Task<bool> Confirm(this IJSRuntime js, string msg) =>
            await js.InvokeAsync<bool>("confirm", msg);

        public static async void SaveFile(this IJSRuntime js, string filename, string data) =>
            await js.InvokeAsync<object>("saveFile", filename, data);

        public static async Task<string> ReadFile(this IJSRuntime js, string control) =>
            await js.InvokeAsync<string>("readFile", control);

        public static string ToJson(DeterministicFiniteAutomaton G)
        {
            var k = 0;
            var statesOrig = G.States.ToDictionary(q => q, q => k++);
            var states = statesOrig.OrderBy(kvp => kvp.Value)
                .Select(kvp => (alias: kvp.Key.ToString(), marking: kvp.Key.Marking))
                .ToArray();

            k = 0;
            var eventsOrig = G.Events.ToDictionary(e => e, e => k++);
            var events = eventsOrig.OrderBy(kvp => kvp.Value)
                .Select(kvp => (alias: kvp.Key.ToString(), controllability: kvp.Key.Controllability))
                .ToArray();

            var transitions = G.Transitions
                .Select(t => (o: statesOrig[t.Origin], t: eventsOrig[t.Trigger], d: statesOrig[t.Destination]))
                .ToArray();

            var aut = (states, events, transitions, initial: statesOrig[G.InitialState], name: G.Name);

            return JsonConvert.SerializeObject(aut);
        }

        public static DeterministicFiniteAutomaton FromJson(string json)
        {
            var aut = JsonConvert.DeserializeObject<((string alias, Marking marking)[] states, (string alias, Controllability controllability)[] events, (int o, int t, int d)[] transitions, int initial, string name)>(json);
            var states = aut.states.Select(q => new State(q.alias, q.marking)).ToArray();
            var events = aut.events.Select(e => new Event(e.alias, e.controllability)).ToArray();
            var transitions = aut.transitions.Select(t => new Transition(states[t.o], events[t.t], states[t.d])).ToArray();
            var initial = states[aut.initial];

            return new DeterministicFiniteAutomaton(transitions, initial, aut.name);
        }

        public static DeterministicFiniteAutomaton Rename(this DeterministicFiniteAutomaton G, string name) =>
            new DeterministicFiniteAutomaton(G.Transitions, G.InitialState, name);

        public static void Serialize<T>(this IJSRuntime ls, T obj, string name)
        {
            var formatter = new XmlSerializer(typeof(T));
            var tw = new StringWriter();
            formatter.Serialize(tw, obj);
            var str = tw.ToString();
            ls.SetItem(name, str);
        }

        public static async Task<T> Deserialize<T>(this IJSRuntime ls, string name)
        {
            var str = await ls.GetItem(name);
            Console.WriteLine(str);

            var formatter = new XmlSerializer(typeof(T));
            var sr = new StringReader(str);

            var obj = (T)formatter.Deserialize(sr);
            return obj;
        }

        public static void SetItem(this IJSRuntime js, string key, string value) =>
            js.InvokeVoidAsync("localStorage.setItem", key, value);

        public static async ValueTask<bool> HasItem(this IJSRuntime js, string key)
        {
            var val = await js.InvokeAsync<string>("localStorage.getItem", key);
            return !string.IsNullOrEmpty(val);
        }

        public static async ValueTask<string> GetItem(this IJSRuntime js, string key) =>
            await js.InvokeAsync<string>("localStorage.getItem", key);

        public static string MaxLength(this string str, int length) =>
            str.Length <= length ? str : str.Substring(0, length - 3) + "...";
    }
}
