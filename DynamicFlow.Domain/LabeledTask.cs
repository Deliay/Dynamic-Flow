using DynamicFlow.Domain.Labels;
using DynamicFlow.Domain.Labels.DefaultMetadata;

namespace DynamicFlow.Domain
{
    public abstract class LabeledTask<T> : DependencyTask<T>, ILabelContainer
        where T : LabeledTask<T>
    {
        private readonly Dictionary<string, HashSet<Label>> _labels;
        private readonly Dictionary<string, Label> _labelMapping;

        public event LabelUpdatedEvent<T>? OnLabelUpdated;
        public event LabelAppliedEvent<T>? OnLabelApplied;

        public LabeledTask()
        {
            _labels = [];
            _labelMapping = [];
        }

        public IEnumerable<Label> GetAllLabels()
        {
            foreach (var category in _labels.Values)
            {
                foreach (var label in category)
                {
                    yield return label;
                }
            }
        }

        private async ValueTask InheritedLabel(HashSet<Label> labels)
        {
            foreach (var label in labels)
            {
                _ = await Add(label);
            }
        }

        private async ValueTask SpreadLabels(T task)
        {
            if (task._labels.TryGetValue(DynFlow.Spread.ToString(), out var extendedLabels))
            {
                foreach (var extendedLabel in extendedLabels)
                {
                    if (task._labels.TryGetValue(extendedLabel.Value, out var labels))
                    {
                        await InheritedLabel(labels);
                    }
                }
            }
        }
        private async ValueTask ExtendLabels(T task)
        {
            if (_labels.TryGetValue(DynFlow.Extends.ToString(), out var extendedLabels))
            {
                foreach (var extendedLabel in extendedLabels)
                {
                    if (task._labels.TryGetValue(extendedLabel.Value, out var labels))
                    {
                        await InheritedLabel(labels);
                    }
                }
            }
        }

        public override async ValueTask ResolveBy(T task)
        {
            await ExtendLabels(task);
            await SpreadLabels(task);
            await base.ResolveBy(task);
        }


        public async ValueTask<bool> Add(Label label)
        {
            if (!_labels.ContainsKey(label.Metadata.ToString()))
            {
                _labels.Add(label.Metadata.ToString(), [label]);
                _labelMapping.Add(label.Id, label);
                await (OnLabelApplied?.Invoke((T)this, label) ?? ValueTask.CompletedTask);
                return true;
            }
            var list = _labels[label.Metadata.ToString()];
            if (label.Metadata.AllowCount == 0 || list.Count < label.Metadata.AllowCount)
            {
                _labelMapping.Add(label.Id, label);
                await (OnLabelApplied?.Invoke((T)this, label) ?? ValueTask.CompletedTask);
                return list.Add(label);
            }
            return false;
        }

        public ValueTask<Label?> Find(LabelMetadata metadata)
        {
            var key = metadata.ToString();
            Label? labelValue = _labels.TryGetValue(key, out HashSet<Label>? value) ? value.FirstOrDefault() : null;

            return ValueTask.FromResult(labelValue);
        }

        public ValueTask<IReadOnlySet<Label>?> FindAll(LabelMetadata metadata)
        {
            var key = metadata.ToString();
            IReadOnlySet<Label>? labelValue = _labels.TryGetValue(key, out HashSet<Label>? value) ? value : null;

            return ValueTask.FromResult(labelValue);
        }

        public async ValueTask Update(Label label)
        {
            await (OnLabelUpdated?.Invoke((T)this, label, _labelMapping[label.Id]) ?? ValueTask.CompletedTask);
            _labelMapping[label.Id].Metadata = label.Metadata;
            _labelMapping[label.Id].Value = label.Value;
        }

        public ValueTask<bool> Remove(Label label)
        {
            var key = label.Metadata.ToString();

            if (!_labels.TryGetValue(key, out HashSet<Label>? value))
                return ValueTask.FromResult(false);

            _labelMapping.Remove(label.Id);
            return ValueTask.FromResult(value.Remove(label));
        }

        public ValueTask<bool> RemoveAll(LabelMetadata label)
        {
            return ValueTask.FromResult(_labels.Remove(label.ToString()));
        }

        public async ValueTask<bool> Contains(LabelMetadata metadata)
        {
            return await Count(metadata) > 0;
        }

        public async ValueTask<string?> Get(LabelMetadata metadata)
        {
            var label = await Find(metadata);
            return label?.Value;
        }

        public async ValueTask<bool> AddOrUpdate(Label label)
        {
            if (label.Metadata.AllowCount == 1)
            {
                if (await Contains(label.Metadata))
                {
                    var key = label.Metadata.ToString();
                    _labels[key].Clear();
                }
            }

            return await Add(label);
        }

        public ValueTask<int> Count(LabelMetadata metadata)
        {
            if (_labels.TryGetValue(metadata.ToString(), out var labels)) return new ValueTask<int>(labels.Count);
            else return new ValueTask<int>(0);
        }
    }

    public delegate ValueTask LabelUpdatedEvent<T>(T task, Label oldLabel, Label newLabel);
    public delegate ValueTask LabelAppliedEvent<T>(T task, Label label);
}
