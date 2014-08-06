﻿using Devkoes.JenkinsClient;
using Devkoes.JenkinsClient.Model;
using Devkoes.JenkinsManagerUI.Managers;
using Devkoes.JenkinsManagerUI.Properties;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Devkoes.JenkinsManagerUI.ViewModels
{
    public class JenkinsManagerViewModel : ViewModelBase
    {
        private bool _showAddJenkinsServer;
        private JenkinsServer _selectedJenkinsServer;
        private string _statusMessage;
        private Job _selectedJob;

        public RelayCommand ShowAddJenkinsForm { get; private set; }
        public RelayCommand SaveJenkinsServer { get; private set; }
        public RelayCommand RemoveJenkinsServer { get; private set; }
        public RelayCommand CancelSaveJenkinsServer { get; private set; }
        public RelayCommand<Job> ScheduleJobCommand { get; private set; }
        public RelayCommand<Job> ShowJobsWebsite { get; private set; }
        public RelayCommand<Job> LinkJobToCurrentSolution { get; private set; }

        public string AddServerUrl { get; set; }
        public string AddServerName { get; set; }

        public ObservableCollection<JenkinsServer> JenkinsServers { get; private set; }
        public ObservableCollection<Job> Jobs { get; private set; }

        public JenkinsManagerViewModel()
        {
            ShowAddJenkinsForm = new RelayCommand(HandleShowAddJenkinsServer);
            SaveJenkinsServer = new RelayCommand(HandleSaveJenkinsServer);
            RemoveJenkinsServer = new RelayCommand(HandleRemoveJenkinsServer);
            CancelSaveJenkinsServer = new RelayCommand(HandleCancelSaveJenkinsServer);
            ScheduleJobCommand = new RelayCommand<Job>(ScheduleJob, CanDoJobAction);
            ShowJobsWebsite = new RelayCommand<Job>(ShowWebsite, CanDoJobAction);
            LinkJobToCurrentSolution = new RelayCommand<Job>(LinkJobToSolution, CanDoJobAction);
            JenkinsServers = new ObservableCollection<JenkinsServer>();
            Jobs = new ObservableCollection<Job>();

            SolutionManager.Instance.SolutionPathChanged += SolutionPathChanged;

            LoadJenkinsServers();
        }

        private void SolutionPathChanged(object sender, SolutionPathChangedEventArgs e)
        {
            UpdateJobLinkedStatus(e.SolutionPath);
        }

        private void UpdateJobLinkedStatus(string slnPath = null)
        {
            if (string.IsNullOrEmpty(slnPath))
            {
                slnPath = SolutionManager.Instance.CurrentSolutionPath;
            }

            string jobUrl = SettingManager.GetJobUri(slnPath);

            foreach (var job in Jobs)
            {
                job.LinkedToCurrentSolution = string.Equals(job.Url, jobUrl, System.StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private void LinkJobToSolution(Job j)
        {
            if (string.IsNullOrEmpty(SolutionManager.Instance.CurrentSolutionPath))
            {
                StatusMessage = Resources.SolutionNotLoaded;
                return;
            }

            SettingManager.SaveJobForSolution(j.Url, SolutionManager.Instance.CurrentSolutionPath);

            UpdateJobLinkedStatus();
        }

        private void ShowWebsite(Job j)
        {
            Process.Start(j.Url);
        }

        private async void ScheduleJob(Job j)
        {
            await ScheduleJob(j.Url);
        }

        public async Task ScheduleJob(string jobUrl)
        {
            try
            {
                await JenkinsManager.ScheduleJob(jobUrl);
            }
            catch (WebException ex)
            {
                var resp = ex.Response as HttpWebResponse;
                if (resp != null)
                {
                    StatusMessage = string.Format(Resources.WebExceptionMessage, "Schedule job", resp.StatusDescription);
                }
                else
                {
                    StatusMessage = string.Format(Resources.WebExceptionMessage, "Schedule job", ex.Status);
                }
            }
        }

        private bool CanDoJobAction(Job arg)
        {
            return SelectedJob != null;
        }

        public Job SelectedJob
        {
            get { return _selectedJob; }
            set
            {
                if (_selectedJob != value)
                {
                    _selectedJob = value;
                    RaisePropertyChanged(() => SelectedJob);
                    ScheduleJobCommand.RaiseCanExecuteChanged();
                    ShowJobsWebsite.RaiseCanExecuteChanged();
                    LinkJobToCurrentSolution.RaiseCanExecuteChanged();
                }
            }
        }

        private void HandleCancelSaveJenkinsServer()
        {
            ShowAddJenkinsServer = false;
            AddServerName = null;
            AddServerUrl = null;
        }

        private void HandleRemoveJenkinsServer()
        {
            JenkinsManager.RemoveServer(SelectedJenkinsServer);
            LoadJenkinsServers();
        }


        public JenkinsServer SelectedJenkinsServer
        {
            get { return _selectedJenkinsServer; }
            set
            {
                _selectedJenkinsServer = value;
                LoadJenkinsJobs();
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                RaisePropertyChanged(() => StatusMessage);
            }
        }

        private async void LoadJenkinsJobs()
        {
            Jobs.Clear();
            if (SelectedJenkinsServer != null)
            {
                var jobs = await JenkinsManager.GetJobs(SelectedJenkinsServer.Url);
                foreach (var job in jobs)
                {
                    Jobs.Add(job);
                }
            }
        }

        private void LoadJenkinsServers()
        {
            JenkinsServers.Clear();
            var servers = JenkinsManager.GetServers();
            foreach (var server in servers)
            {
                if (SelectedJenkinsServer == null)
                {
                    SelectedJenkinsServer = server;
                }
                JenkinsServers.Add(server);
            }
        }

        private void HandleSaveJenkinsServer()
        {
            JenkinsManager.AddServer(new JenkinsServer() { Name = AddServerName, Url = AddServerUrl });
            LoadJenkinsServers();
            ShowAddJenkinsServer = false;
        }

        private void HandleShowAddJenkinsServer()
        {
            ShowAddJenkinsServer = !ShowAddJenkinsServer;
        }

        public bool ShowAddJenkinsServer
        {
            get { return _showAddJenkinsServer; }
            set
            {
                _showAddJenkinsServer = value;
                RaisePropertyChanged(() => ShowAddJenkinsServer);
            }
        }

    }
}
